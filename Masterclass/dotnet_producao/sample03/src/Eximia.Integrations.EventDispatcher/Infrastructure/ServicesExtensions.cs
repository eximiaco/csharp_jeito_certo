using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Web;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Serilog;
using Serilog.Exceptions;
using Serilog.Filters;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.HealthChecks;
using Eximia.Integrations.Shared.Kafka;
using Eximia.Integrations.Shared.Postgres;
using Eximia.Integrations.Auth;
using Eximia.Integrations.IntegrationEvents.Application;
using Eximia.Integrations.IntegrationEvents.Application.Routing;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.EventDispatcher.Infrastructure;

internal static class ServicesExtensions
{

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

        services
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddKafka())
            .AddBrokerClientsConfigurator<KafkaClientsConfigurator>()
            .AddScopedSubscriber<RouteIntegrationEventConsumer>()
            .AddScopedSubscriber<ProcessIntegrationEventCallbackApplication>();

        return services;
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
                    builder => { builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader(); }
                )
        );
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

    public static IServiceCollection AddHttpClients(this IServiceCollection services)
        => services;

    public static IServiceCollection AddWorkersServices(this IServiceCollection services, IConfiguration configuration)
    {
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
            .Enrich.WithThreadId()
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
