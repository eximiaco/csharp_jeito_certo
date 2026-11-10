using Npgsql;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Datadog.OpenTelemetry;
using Serilog.Exceptions;
using Serilog.Sinks.OpenTelemetry;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Messaging.HealthChecks;
using Silverback.Storage;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Kafka;
using Eximia.Integrations.Shared.OpenTelemetry;
using Eximia.Integrations.Shared.Postgres;
using Eximia.Integrations.HttpApi.Infrastructure.ProducerBehaviors;
using Eximia.Integrations.IntegrationEvents;
using Eximia.Integrations.Outbox;
using Eximia.Integrations.Outbox.EntityFramework;
using Eximia.Integrations.Routing;

namespace Eximia.Integrations.HttpApi.Infrastructure;

internal static class ServicesExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection serviceCollection, string serviceName,
        string serviceVersion, IConfiguration configuration)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        Log.Information("ASPNETCORE_ENVIRONMENT -> " + environmentName);

        TelemetrySettings settings;
        if (configuration.GetSection("OpenTelemetry") is var section && section.Exists())
            settings = new TelemetrySettings(serviceName, serviceVersion,
                new TelemetryExporter(section["Type"] ?? string.Empty, section["Endpoint"] ?? string.Empty));
        else
            settings = new TelemetrySettings(serviceName, serviceVersion,
                new TelemetryExporter("console", ""));
        serviceCollection.AddSingleton(settings);
        serviceCollection.AddSingleton(sp => new OtelTracingService(sp.GetService<TelemetrySettings>()));   // dono do ActivitySource: singleton
        Action<ResourceBuilder> configureResource = r => r
            .AddService(
                serviceName: settings.ServiceName,
                serviceVersion: settings.ServiceVersion,
                serviceInstanceId: Environment.MachineName)
            .AddAttributes(new Dictionary<string, object> { ["deployment.environment"] = environmentName });
        serviceCollection
            .AddOpenTelemetry()
            .ConfigureResource(configureResource)
            .WithTracing(builder =>
            {
                builder
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource(settings.ServiceName)
                    .AddSource("Silverback.Integration.Produce")
                    .AddHttpClientInstrumentation(o =>
                        o.FilterHttpWebRequest = request => !request.Address.AbsoluteUri.Contains("/raw"))
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        opts.EnrichWithHttpRequest = (a, r) => a?.AddTag("env", environmentName);

                    });
                switch (settings.Exporter.Type.ToLower())
                {
                    case "otlp":
                        builder.AddOtlpExporter(config =>
                        {
                            config.Endpoint = new Uri(settings.Exporter.Endpoint ?? string.Empty);
                            //config.ExportProcessorType = ExportProcessorType.Simple;
                            config.Protocol = OtlpExportProtocol.Grpc;
                        });
                        break;
                    default:
                        builder.AddConsoleExporter();
                        break;
                }
            })
            .WithMetrics(builder =>
            {
                builder
                    .ConfigureResource(configureResource)
                    .AddMeter(PlatformMetrics.MeterName)
                    .AddMeter(OutboxMetrics.MeterName)
                    //.AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation();

                switch (settings.Exporter.Type.ToLower())
                {
                    case "otlp":
                        builder.AddOtlpExporter(config =>
                        {
                            config.Endpoint = new Uri(settings.Exporter.Endpoint ?? string.Empty);
                        });
                        break;
                    default:
                        builder.AddConsoleExporter();
                        break;
                }
            });
        return serviceCollection;
    }

    public static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection kafkaSection = configuration.GetSection("Kafka");
        var kafkaConfig = new KafkaConfig();
        kafkaConfig.Connection = kafkaSection.GetSection("Connection").Get<KafkaConnectionConfig>()!;
        kafkaConfig.Callback = kafkaSection.GetSection("Callback").Get<KafkaCallbackConfig>()!;
        kafkaConfig.Events = kafkaSection.GetSection("Events").Get<KafkaEventConfig>()!;

        services.AddSingleton(kafkaConfig);
        services.AddScoped<TopicNameFactoryPeerEnviroment>();

        var connectionString = new PostgresConnectionStringFactory(configuration).CreateConnectionString().ConnectionString;

        services
            .AddSilverback()
            .AddSingletonBrokerBehavior<AddDestinationHeaderProducerBehavior>()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddPostgreSqlOutbox()
                .AddOutboxWorker(worker => worker
                    .WithInterval(TimeSpan.FromMilliseconds(500))
                    .WithBatchSize(1000)
                    .EnforceMessageOrder()
                    .WithDistributedLock(distributedLock => distributedLock
                        .UsePostgreSqlAdvisoryLock(OutboxDefaults.LockName, connectionString))
                    .ProcessOutbox(outbox => outbox.UsePostgreSql(connectionString))))
            .AddBrokerClientsConfigurator<KafkaClientsConfigurator>();

        // Faz o OutboxWorker padrão ler o outbox pelo EximiaOutboxReader (status InQueue + métricas).
        services.UseEximiaOutboxReader();

        return services;
    }

    /// <summary>
    ///     Cria (se necessário) a tabela do outbox do Silverback. Deve ser chamado antes de iniciar o host.
    /// </summary>
    public static async Task EnsureOutboxTableAsync(this IServiceProvider serviceProvider, IConfiguration configuration)
    {
        var connectionString = new PostgresConnectionStringFactory(configuration).CreateConnectionString().ConnectionString;

        await new SilverbackStorageInitializer(serviceProvider)
            .CreatePostgreSqlOutboxAsync(connectionString, OutboxDefaults.TableName, TimeSpan.FromSeconds(30));
    }


    public static IServiceCollection AddSwaggerDoc(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.CustomSchemaIds(x => x.ToString());
            c.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Description =
                        "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                }
            );

            c.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                }
            );

            c.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "Cdc Consumer",
                    Description = "Consumer consolidator worker.",
                    Version = "v1"
                }
            );
        });
        return services;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services)
    {
        services.AddCors(
            o =>
                o.AddPolicy(
                    "default",
                    builder =>
                    {
                        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                    }
                )
        );
        return services;
    }

    public static IServiceCollection AddHealth(this IServiceCollection services, IConfiguration configuration)
    {
        var hcBuilder = services.AddHealthChecks();
        var connectionStringFactory = new PostgresConnectionStringFactory(configuration);

        // O health check do outbox do Silverback depende de IOutboxReader, e o Silverback NÃO registra esse
        // serviço no container. Sem esta linha o /health-check quebrava na primeira chamada: quem revelou foi a
        // validação de container na subida.
        services.AddSingleton<IOutboxReader>(_ => new PostgreSqlOutboxReader(
            new PostgreSqlOutboxSettings(connectionStringFactory.CreateConnectionString().ConnectionString)));

        hcBuilder
            .AddNpgSql(
                connectionStringFactory.CreateConnectionString().ConnectionString,
                name: "integration-store-check",
                tags: ["ready"])
            .AddOutboxCheck(
                name: "integration-kafka-check",
                tags: ["kafka-healthy"]);

        return services;
    }

    internal static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<List<RouteAuthenticationSettings>>(configuration.GetSection("RouteAuthentication"));
        return services;
    }

    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(configuration);
        return services;
    }

    public static IServiceCollection AddLogs(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithDatadogTraceId()
            .Enrich.WithDatadogSpanId()
            .Enrich.WithExceptionDetails()
            .WriteTo.OpenTelemetry(
                endpoint: "http://localhost:4317",
                protocol: OtlpProtocol.Grpc)
            .CreateLogger();
        services.AddSingleton(Log.Logger);
        return services;
    }

    public static IServiceCollection AddCaching(this IServiceCollection services)
    {
        services.AddSingleton<IMemoryCache>(
            o =>
            {
                return new MemoryCache(new MemoryCacheOptions());
            });
        return services;
    }

    public static IServiceCollection AddEfDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        // O EF usa o MESMO data source (e o mesmo pool) do acesso via Dapper.
        services.AddDbContextPool<EximiaIntegrationDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>()));
        return services;
    }

    internal static IServiceCollection AddGoogleStorageAuth(this IServiceCollection services, IConfiguration configuration)
    {
        StorageOptions jsonParameters = new();
        configuration.GetSection("GoogleStorage").Bind(jsonParameters);
        var googleCredential = GoogleCredential.FromJson(JsonConvert.SerializeObject(jsonParameters));

        services.AddSingleton(sr => googleCredential);
        
        services.AddSingleton(provider =>
        {
            var credential = provider.GetRequiredService<GoogleCredential>();
            return StorageClient.Create(credential);
        });
        
        services.AddSingleton(provider =>
        {
            var credential = provider.GetRequiredService<GoogleCredential>();
            return UrlSigner.FromCredential(credential);
        });
        
        return services;
    }
}
