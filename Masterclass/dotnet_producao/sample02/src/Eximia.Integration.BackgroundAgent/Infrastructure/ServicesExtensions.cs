using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Serilog;
using Serilog.Exceptions;
using Serilog.Filters;
using Eximia.Integrations.Shared.Kafka;
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

        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddBrokerClientsConfigurator<KafkaClientsConfigurator>();

        return services;
    }


    internal static IServiceCollection AddEfDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextPool<EximiaIntegrationDbContext>(options =>
        {
            options.UseNpgsql(new PostgresConnectionStringFactory(configuration).CreateConnectionString().ConnectionString);
        });
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
            .Enrich.WithThreadId()
            .Enrich.WithExceptionDetails()
            .Filter.ByExcluding(
                Matching.FromSource("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager")
            )
            .CreateLogger();
        services.AddSingleton(Log.Logger);
        return services;
    }
}
