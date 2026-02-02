using GymErp.Common;
using GymErp.Common.Infrastructure;
using GymErp.Common.Kafka;
using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Financial.Features.ProcessCharging.Consumers;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using MassTransit;

namespace GymErp.Bootstrap;

internal static class MassTransitServiceExtensions
{
    static MassTransitServiceExtensions()
    {
        MessageBrokerRegistry.Register("MassTransit", (s, c) => s.AddMassTransitBroker(c));
    }

    public static IServiceCollection AddMassTransitBroker(this IServiceCollection services, IConfiguration configuration)
    {
        AddMassTransitKafka(services, configuration);
        services.AddScoped<IServiceBus, MassTransitServiceBus>();
        return services;
    }

    public static IServiceCollection AddMassTransitKafka(this IServiceCollection services, IConfiguration configuration)
    {
        var kafkaSection = configuration.GetSection("Kafka");
        var kafkaConfig = new KafkaConfig();
        kafkaConfig.Connection = kafkaSection.GetSection("Connection").Get<KafkaConnectionConfig>()!;
        services.AddSingleton(kafkaConfig);

        var bootstrapServers = kafkaConfig.Connection.BootstrapServers;

        services.AddMassTransit(x =>
        {
            x.UsingInMemory();

            x.AddRider(rider =>
            {
                rider.AddConsumer<EnrollmentCreatedEventMassTransitConsumer>();
                rider.AddProducer<EnrollmentCreatedEvent>("enrollment-events");
                rider.AddProducer<ChargingProcessedEvent>("charging-processed-events");

                rider.UsingKafka((context, k) =>
                {
                    k.Host(bootstrapServers);
                    k.TopicEndpoint<EnrollmentCreatedEvent>("enrollment-events", "financial-module", e =>
                    {
                        e.ConfigureConsumer<EnrollmentCreatedEventMassTransitConsumer>(context);
                    });
                });
            });
        });

        return services;
    }
}
