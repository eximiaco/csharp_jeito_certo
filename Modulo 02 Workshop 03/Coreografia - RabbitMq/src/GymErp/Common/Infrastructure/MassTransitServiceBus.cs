using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment;
using MassTransit;

namespace GymErp.Common.Infrastructure;

public class MassTransitServiceBus(IPublishEndpoint publishEndpoint) : IServiceBus
{
    public async Task PublishAsync(object message)
    {
        switch (message)
        {
            case EnrollmentCreatedEvent enrollmentCreated:
                await publishEndpoint.Publish(enrollmentCreated);
                break;
            case ChargingProcessedEvent chargingProcessed:
                await publishEndpoint.Publish(chargingProcessed);
                break;
            case CancelEnrollmentCommand cancelEnrollmentCommand:
                await publishEndpoint.Publish(cancelEnrollmentCommand);
                break;
            default:
                throw new ArgumentException($"Unsupported message type: {message.GetType().Name}", nameof(message));
        }
    }
}
