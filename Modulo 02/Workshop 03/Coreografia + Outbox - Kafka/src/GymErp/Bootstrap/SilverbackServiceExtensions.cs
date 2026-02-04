using Confluent.Kafka;
using GymErp.Common;
using GymErp.Common.Infrastructure;
using GymErp.Common.Kafka;
using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Financial.Features.ProcessCharging.Consumers;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment.Consumers;
using Silverback.Messaging.Configuration;

namespace GymErp.Bootstrap;

internal static class SilverbackServiceExtensions
{
    static SilverbackServiceExtensions()
    {
        MessageBrokerRegistry.Register("Silverback", (s, c) => s.AddSilverbackBroker(c));
    }

    public static IServiceCollection AddSilverbackBroker(this IServiceCollection services, IConfiguration configuration)
    {
        AddSilverbackKafka(services, configuration);
        services.AddScoped<IServiceBus, SilverbackServiceBus>();
        return services;
    }

    public static IServiceCollection AddSilverbackKafka(this IServiceCollection services, IConfiguration configuration)
    {
        var kafkaSection = configuration.GetSection("Kafka");
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
                .AddOutbound<EnrollmentSuspendedEvent>(endpoint => endpoint
                    .ProduceTo("enrollment-suspended-events")
                    .WithKafkaKey<EnrollmentSuspendedEvent>(envelope => envelope.Message!.EnrollmentId)
                    .SerializeAsJson(serializer => serializer.UseFixedType<EnrollmentSuspendedEvent>())
                    .DisableMessageValidation())
                .AddOutbound<ChargingProcessedEvent>(endpoint => endpoint
                    .ProduceTo("charging-processed-events")
                    .WithKafkaKey<ChargingProcessedEvent>(envelope => envelope.Message!.EnrollmentId)
                    .SerializeAsJson(serializer => serializer.UseFixedType<ChargingProcessedEvent>())
                    .DisableMessageValidation())
                .AddOutbound<CancelEnrollmentCommand>(endpoint => endpoint
                    .ProduceTo("cancel-enrollment-commands")
                    .WithKafkaKey<CancelEnrollmentCommand>(envelope => envelope.Message!.EnrollmentId)
                    .SerializeAsJson(serializer => serializer.UseFixedType<CancelEnrollmentCommand>())
                    .DisableMessageValidation())
                .AddInbound<EnrollmentCreatedEvent>(endpoint => endpoint
                    .ConsumeFrom("enrollment-events")
                    .Configure(config =>
                    {
                        config.GroupId = "financial-module";
                        config.AutoOffsetReset = AutoOffsetReset.Latest;
                    })
                    .DisableMessageValidation())
                .AddInbound<CancelEnrollmentCommand>(endpoint => endpoint
                    .ConsumeFrom("cancel-enrollment-commands")
                    .Configure(config =>
                    {
                        config.GroupId = "subscriptions-module";
                        config.AutoOffsetReset = AutoOffsetReset.Latest;
                    })
                    .DisableMessageValidation()))
            .AddScopedSubscriber<EnrollmentCreatedEventConsumer>()
            .AddScopedSubscriber<CancelEnrollmentCommandSilverbackConsumer>();

        return services;
    }
}
