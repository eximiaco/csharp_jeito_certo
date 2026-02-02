using System.Text;
using Asp.Versioning;
using Confluent.Kafka;
using FinanceManager.Application;
using FinanceManager.Controllers;
using FinanceManager.Domain;
using FinanceManager.Infra.Auth;
using FinanceManager.Infra.HostedServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.CircuitBreaker;
using Serilog;

namespace FinanceManager.Infra;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<CacheProvider>();
        services.AddScoped<IdentityUserService>();

        services.AddScoped(_ => new ProducerBuilder<Null, string>(
            new ProducerConfig { BootstrapServers = configuration.GetValue<string>("Kafka") }).Build()
        );

        services.AddHttpClient<ExchangeService>()
            .ConfigureHttpClient((serviceProvider, httpCLient) =>
            {
                httpCLient.Timeout = TimeSpan.FromSeconds(30);
            });

        services.Configure<ExchangeServiceOptions>(configuration.GetSection("ExchangeService"));
        services.AddScoped<ExchangeService>();

        services.AddScoped<IMessageBroker, MessageBrokerService>();

        // Registro dos handlers
        services.AddScoped<CreateBankAccountCommandHandler>();
        services.AddScoped<CreateTransactionCommandHandler>();
        services.AddScoped<CreateBudgetAlertaCommandHandler>();
        services.AddScoped<CreateTransactionCategoryCommandHandler>();
        services.AddScoped<CreateUserCommandHandler>();
        services.AddScoped<LoginCommandHandler>();

        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<FinanceManagerDbContext>(opts =>
            opts.UseSqlServer(connectionString, sqlOptions => sqlOptions.CommandTimeout(30))
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

    public static IServiceCollection AddVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(opts =>
        {
            opts.DefaultApiVersion = new ApiVersion(1);
            opts.ReportApiVersions = true;
            opts.AssumeDefaultVersionWhenUnspecified = true;
            opts.ApiVersionReader = new UrlSegmentApiVersionReader();
        })
        .AddMvc()
        .AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'V";
            options.SubstituteApiVersionInUrl = true;
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
}