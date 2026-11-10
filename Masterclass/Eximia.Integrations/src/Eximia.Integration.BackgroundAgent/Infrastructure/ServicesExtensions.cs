using Npgsql;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Enrichers.Datadog.OpenTelemetry;
using Serilog.Exceptions;
using Serilog.Filters;
using Eximia.Integrations.Shared.Kafka;
using Eximia.Integrations.Shared.OpenTelemetry;
using Eximia.Integrations.Shared.Postgres;
using Eximia.Integrations.Auth;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Outbox.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace Eximia.Integration.BackgroundAgent.Infrastructure;

internal static class ServicesExtensions
{
    internal static IServiceCollection AddKafka(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<KafkaConfig>(configuration.GetSection("Kafka"));

        IConfigurationSection kafkaSection = configuration.GetSection("Kafka");
        var kafkaConfig = new KafkaConfig();
        kafkaConfig.Connection = kafkaSection.GetSection("Connection").Get<KafkaConnectionConfig>()!;
        kafkaConfig.Events = kafkaSection.GetSection("Events").Get<KafkaEventConfig>()!;

        services.AddSingleton(kafkaConfig);
        services.AddScoped<TopicNameFactoryPeerEnviroment>();

        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddBrokerClientsConfigurator<KafkaClientsConfigurator>();

        return services;
    }

    internal static IServiceCollection AddTelemetry(this IServiceCollection serviceCollection, string serviceName,
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
            .WithMetrics(builder =>
            {
                builder
                    .ConfigureResource(configureResource)
                    .AddMeter(PlatformMetrics.MeterName)
                    .AddRuntimeInstrumentation();
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
            })
            .WithTracing(builder =>
            {
                builder
                    .SetSampler(new AlwaysOnSampler())
                    .AddSource(settings.ServiceName);
            });
        return serviceCollection;
    }

    internal static IServiceCollection AddEfDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        // O EF usa o MESMO data source (e o mesmo pool) do acesso via Dapper.
        services.AddDbContextPool<EximiaIntegrationDbContext>((sp, options) =>
            options.UseNpgsql(sp.GetRequiredService<NpgsqlDataSource>()));
        return services;
    }

    internal static IServiceCollection AddCustomOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthSettings>(configuration.GetSection("Auth"));
        services.Configure<List<RouteAuthenticationSettings>>(configuration.GetSection("RouteAuthentication"));
        return services;
    }

    internal static IServiceCollection AddLogs(this IServiceCollection services, IConfiguration configuration)
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
}
