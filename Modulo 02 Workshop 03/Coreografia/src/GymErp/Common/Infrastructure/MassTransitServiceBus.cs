using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using MassTransit;

namespace GymErp.Common.Infrastructure;

public class MassTransitServiceBus(
    ITopicProducer<EnrollmentCreatedEvent> enrollmentCreatedProducer,
    ITopicProducer<ChargingProcessedEvent> chargingProcessedProducer) : IServiceBus
{
    public async Task PublishAsync(object message)
    {
        switch (message)
        {
            case EnrollmentCreatedEvent enrollmentCreated:
                await enrollmentCreatedProducer.Produce(enrollmentCreated);
                break;
            case ChargingProcessedEvent chargingProcessed:
                await chargingProcessedProducer.Produce(chargingProcessed);
                break;
            default:
                throw new ArgumentException($"Unsupported message type: {message.GetType().Name}", nameof(message));
        }
    }
}
