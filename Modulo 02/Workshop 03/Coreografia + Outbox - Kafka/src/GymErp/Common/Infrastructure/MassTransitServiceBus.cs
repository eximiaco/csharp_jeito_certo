using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment;
using MassTransit;

namespace GymErp.Common.Infrastructure;

public class MassTransitServiceBus(
    ITopicProducer<EnrollmentCreatedEvent> enrollmentCreatedProducer,
    ITopicProducer<EnrollmentSuspendedEvent> enrollmentSuspendedProducer,
    ITopicProducer<ChargingProcessedEvent> chargingProcessedProducer,
    ITopicProducer<CancelEnrollmentCommand> cancelEnrollmentCommandProducer) : IServiceBus
{
    public async Task PublishAsync(object message)
    {
        switch (message)
        {
            case EnrollmentCreatedEvent enrollmentCreated:
                await enrollmentCreatedProducer.Produce(enrollmentCreated);
                break;
            case EnrollmentSuspendedEvent enrollmentSuspended:
                await enrollmentSuspendedProducer.Produce(enrollmentSuspended);
                break;
            case ChargingProcessedEvent chargingProcessed:
                await chargingProcessedProducer.Produce(chargingProcessed);
                break;
            case CancelEnrollmentCommand cancelEnrollmentCommand:
                await cancelEnrollmentCommandProducer.Produce(cancelEnrollmentCommand);
                break;
            default:
                throw new ArgumentException($"Unsupported message type: {message.GetType().Name}", nameof(message));
        }
    }
}
