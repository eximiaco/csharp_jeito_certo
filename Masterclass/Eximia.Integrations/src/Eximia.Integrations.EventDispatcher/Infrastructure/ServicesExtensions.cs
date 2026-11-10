using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Newtonsoft.Json;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Datadog.OpenTelemetry;
using Serilog.Exceptions;
using Serilog.Filters;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.HealthChecks;
using Eximia.Integrations.Shared.Kafka;
using Eximia.Integrations.Shared.OpenTelemetry;
using Eximia.Integrations.Shared.Postgres;
using Eximia.Integrations.Auth;
using Eximia.Integrations.EventDispatcher.Infrastructure.ConsumerBehaviors;
using Eximia.Integrations.IntegrationEvents.Application;
using Eximia.Integrations.IntegrationEvents.Application.Routing;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.EventDispatcher.Infrastructure;

internal static class ServicesExtensions
{
    public static IServiceCollection AddTelemetry(this IServiceCollection serviceCollection, string serviceName,
            string serviceVersion, IConfiguration configuration)
    {
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        TelemetrySettings settings;
        if (configuration.GetSection("OpenTelemetry") is var section && section.Exists())
            settings = new TelemetrySettings(serviceName, serviceVersion,
                new TelemetryExporter(section["Type"] ?? string.Empty, section["Endpoint"] ?? string.Empty));
        else
            settings = new TelemetrySettings(serviceName, serviceVersion,
                new TelemetryExporter("console", ""));
        serviceCollection.AddSingleton(settings);
        serviceCollection.AddSingleton(sp => new OtelTracingService(sp.GetService<TelemetrySettings>()));
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
                    .AddSource(settings.ServiceName)
                    .AddSource("Silverback.Integration.Consume")
                    .AddHttpClientInstrumentation()
                    .AddAspNetCoreInstrumentation(opts =>
                    {
                        //opts.EnrichWithHttpRequest = (a, r) => a?.AddTag("env", environmentName);
                    });
                switch (settings.Exporter.Type.ToLower())
                {
                    case "otlp":
                        builder.AddOtlpExporter(config =>
                        {
                            config.Endpoint = new Uri(settings.Exporter.Endpoint ?? string.Empty);
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
                    .AddRuntimeInstrumentation()
                    .AddAspNetCoreInstrumentation();
                switch (settings.Exporter.Type.ToLower())
                {
                    case "otlp":
                        builder.AddOtlpExporter(config =>
                        {
                            config.Endpoint = new Uri(settings.Exporter.Endpoint ?? string.Empty);
                            //config.ExportProcessorType = ExportProcessorType.Simple;
                            //config.Protocol = OtlpExportProtocol.Grpc;
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
        services.Configure<KafkaConfig>(configuration.GetSection("Kafka"));

        IConfigurationSection kafkaSection = configuration.GetSection("Kafka");
        var kafkaConfig = new KafkaConfig();
        kafkaConfig.Connection = kafkaSection.GetSection("Connection").Get<KafkaConnectionConfig>()!;
        kafkaConfig.Consumer.Inbounds.AddRange(kafkaSection.GetSection("Consumer:Inbounds").Get<List<KafkaInboundConfig>>()!);
        kafkaConfig.Callback = kafkaSection.GetSection("Callback").Get<KafkaCallbackConfig>()!;
        kafkaConfig.Events = kafkaSection.GetSection("Events").Get<KafkaEventConfig>()!;

        services.AddSingleton(kafkaConfig);
        services.AddScoped<TopicNameFactoryPeerEnviroment>();

        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddBrokerClientsConfigurator<KafkaClientsConfigurator>()
            .AddSingletonBrokerBehavior<LoggingBehavior>()
            .AddScopedSubscriber<RouteIntegrationEventConsumer>()
            .AddScopedSubscriber<ProcessIntegrationEventCallbackApplication>();

        return services;
    }

    public static IServiceCollection AddHealth(this IServiceCollection services, IConfiguration configuration)
    {
        var hcBuilder = services.AddHealthChecks();
        var connectionStringFactory = new PostgresConnectionStringFactory(configuration);

        hcBuilder
            .AddNpgSql(
                connectionStringFactory.CreateConnectionString().ConnectionString,
                name: "integration-store-check",
                tags: ["ready"])
            .AddConsumersCheck(
                name: "kafka-consumers-check",
                tags: ["kafka-healthy"]);

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
            .Filter.ByExcluding(
                Matching.FromSource("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager")
            ) 
            .CreateLogger();
        services.AddSingleton(Log.Logger);
        return services;
    }

    public static IServiceCollection AddCaching(this IServiceCollection services)
    {
        services.AddMemoryCache();
        return services;
    }

    internal static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthSettings>(configuration.GetSection("Auth"));
        services.Configure<List<RouteAuthenticationSettings>>(configuration.GetSection("RouteAuthentication"));
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
