using Confluent.Kafka;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using Serilog;
using GymErp.Common.Kafka;
using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using Silverback.Messaging.Configuration;

namespace GymErp.Bootstrap;

internal static class ServicesExtensions
{
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

        return services;
    }

    public static IServiceCollection AddWorkersServices(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }

    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
       
        return services;
    }

    public static IServiceCollection AddLogs(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
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
        //services.AddMemoryCache();
        return services;
    }

    public static IServiceCollection AddSilverbackKafka(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection kafkaSection = configuration.GetSection("Kafka");
        var kafkaConfig = new KafkaConfig();
        kafkaConfig.Connection = kafkaSection.GetSection("Connection").Get<KafkaConnectionConfig>()!;
        services.AddSingleton(kafkaConfig);

        services
            .AddSilverback()
            .WithConnectionToMessageBroker(config => config.AddKafka())
            .AddKafkaEndpoints(endpoints => endpoints
                .Configure(config =>
                {
                    config.BootstrapServers = kafkaConfig.Connection.BootstrapServers;
                    config.ClientId = kafkaConfig.Connection.ClientId;
                })
                .AddOutbound<EnrollmentCreatedEvent>(endpoint => endpoint
                    .ProduceTo("enrollment-events")
                    .WithKafkaKey<EnrollmentCreatedEvent>(envelope => envelope.Message!.EnrollmentId)
                    .SerializeAsJson(serializer => serializer.UseFixedType<EnrollmentCreatedEvent>())
                    .DisableMessageValidation())
                .AddInbound<EnrollmentCreatedEvent>(endpoint => endpoint
                    .ConsumeFrom("enrollment-events")
                    .Configure(config =>
                    {
                        config.GroupId = "financial-module";
                        config.AutoOffsetReset = AutoOffsetReset.Latest;
                    })
                    .DisableMessageValidation()))
            .AddScopedSubscriber<GymErp.Domain.Financial.Features.ProcessCharging.Handler>();

        return services;
    }
}