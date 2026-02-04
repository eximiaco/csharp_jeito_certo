using System.Reflection;
using System.Text;
using Asp.Versioning;
using FinanceManager.Crosscutting.Auth;
using FinanceManager.Crosscutting.Database;
using FinanceManager.Crosscutting.Filters;
using FinanceManager.Crosscutting.HostedServices;
using FinanceManager.Crosscutting.Messaging;
using FinanceManager.UserAccounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.CircuitBreaker;
using RabbitMQ.Client;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.OpenTelemetry;

namespace FinanceManager.Crosscutting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogging(this IServiceCollection services, IConfiguration configuration)
    {
        var apiUrl = configuration.GetValue<string>("Seq:Endpoint") ?? throw new InvalidOperationException("Seq Endpoint not found");
        var apiKey = configuration.GetValue<string>("Seq:ApiKey") ?? throw new InvalidOperationException("Seq ApiKey not found");
        var serviceName = configuration.GetValue<string>("Seq:ServiceName") ?? throw new InvalidOperationException("Seq ServiceName not found");

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails()
#if DEBUG
            .WriteTo.Console()
#endif
            .WriteTo.OpenTelemetry(cfg =>
            {
                cfg.Endpoint = apiUrl;
                cfg.Protocol = OtlpProtocol.HttpProtobuf;
                cfg.Headers = new Dictionary<string, string>
                {
                    ["X-Seq-ApiKey"] = apiKey
                };
                cfg.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName
                };
            })
            .CreateLogger();

        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<EventDispatcherService>();
        services.AddScoped<IdentityUserService>();
        services.AddScoped<QueueService>();

        var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMQ")
            ?? throw new InvalidOperationException("RabbitMQ ConnectionString not found");

        services.AddSingleton((_) =>
        {
            return new ConnectionFactory { HostName = rabbitMqConnectionString }.CreateConnectionAsync().Result;
        });

        services.AddHttpClient<ExchangeService>()
            .ConfigureHttpClient((serviceProvider, httpCLient) =>
            {
                httpCLient.Timeout = TimeSpan.FromSeconds(30);
            });

        services.AddOptions<ExchangeServiceOptions>()
            .BindConfiguration(ExchangeServiceOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ExchangeService>();

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<FinanceManagerDbContext>(opts =>
            opts.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(1000);
                sqlOptions.EnableRetryOnFailure(10);
            }).EnableSensitiveDataLogging(true)
        );

        services.AddDbContext<IdentityDbContext>(opts =>
            opts.UseSqlServer(connectionString, sqlOptions => sqlOptions.CommandTimeout(30))
        );

        return services;
    }

    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services)
    {
        services.AddResiliencePipeline("circuit-breaker", builder =>
        {
            var cbOptions = new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.7,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(10),
                OnOpened = (args) =>
                {
                    Log.Logger.Information("Exchange service CB entered open state");
                    return ValueTask.CompletedTask;
                },
                OnHalfOpened = (args) =>
                {
                    Log.Logger.Information("Exchange service CB entered on half opened state");
                    return ValueTask.CompletedTask;
                },
                OnClosed = (args) =>
                {
                    Log.Logger.Information("Exchange service CB has closed");
                    return ValueTask.CompletedTask;
                },
            };

            builder.AddCircuitBreaker(cbOptions);
        });

        return services;
    }

    public static IServiceCollection AddAuthorizationAndAuthentication(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        var authenticationSection = configuration.GetSection("Authentication");
        var basicAuthenticationSection = configuration.GetSection("BasicAuthentication");

        serviceCollection.AddOptions<BearerAuthenticationOptions>()
            .Bind(authenticationSection)
            .ValidateOnStart();

        serviceCollection.AddOptions<BasicAuthenticationOptions>()
            .Bind(basicAuthenticationSection)
            .ValidateOnStart();

        serviceCollection.AddOptions<CreateAdminUserOptions>()
            .Bind(configuration.GetSection("AdminCredentials"));

        var authenticationOptions = authenticationSection.Get<BearerAuthenticationOptions>()!;

        serviceCollection
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("Basic", null)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(authenticationOptions.Secret)),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = authenticationOptions.Issuer,
                    ValidAudience = authenticationOptions.Audience,
                    ClockSkew = TimeSpan.Zero
                };
            });

        serviceCollection.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .Build();

            options.AddPolicy("Admin", policy =>
            {
                policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                policy.RequireClaim("Role", "Admin");
                policy.RequireAuthenticatedUser();
            });
        });

        serviceCollection
            .AddIdentity<UserAccount, IdentityRole>()
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        serviceCollection.AddScoped<ITokenProviderService, TokenProviderService>();

        return serviceCollection;
    }

    public static void UseEndpoints(this WebApplication app)
    {
        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .HasApiVersion(new ApiVersion(2))
            .ReportApiVersions()
            .Build();

        var globalGroup = app.MapGroup(prefix: string.Empty)
            .AddEndpointFilter<NormalizeBadRequestErrorsFilter>()
            .MapGroup("api/v{version:apiVersion}")
            .WithApiVersionSet(apiVersionSet)
            .MapToApiVersion(1);

        var endpoints = Assembly.GetAssembly(typeof(Program))!
            .DefinedTypes
            .Where(type => type is { IsInterface: false, IsAbstract: false } && type.IsAssignableTo(typeof(IEndpoint)))
            .Select(type => Activator.CreateInstance(type) as IEndpoint ?? throw new InvalidOperationException($"Could not create instance of IEndpoint {type.Name}"))
            .ToArray();

        foreach (var endpoint in endpoints)
        {
            endpoint.Map(globalGroup);
        }
    }
}