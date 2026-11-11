using Npgsql;
using Hangfire;
using Hangfire.Console;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Exceptions;
using Serilog.Filters;
using Eximia.Integrations.Shared.Kafka;
using Eximia.Integrations.Shared.Postgres;
using Eximia.Integrations.Auth;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Jobs;
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
        return services;
    }

    internal static IServiceCollection AddHangfireJobs(this IServiceCollection services, IConfiguration configuration)
    {
        JobsHealth jobsHealth = new([]);
        services.AddSingleton(jobsHealth);

        services
            .AddHangfire(config => config
            .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(new PostgresConnectionStringFactory(configuration)
               .CreateConnectionString().ConnectionString), new PostgreSqlStorageOptions
               {
                   InvisibilityTimeout = TimeSpan.FromHours(12)
               })
           .UseConsole());

        services.AddHangfireServer();

        services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme);

        services.AddAuthorization(options =>
        {
            options.AddPolicy("HangfirePolicy", builder =>
            {
                builder
                 .AddAuthenticationSchemes(OpenIdConnectDefaults.AuthenticationScheme)
                 .RequireAuthenticatedUser();

            });
        });

        services.AddHostedService<JobsConfiguration>();
        return services;
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
