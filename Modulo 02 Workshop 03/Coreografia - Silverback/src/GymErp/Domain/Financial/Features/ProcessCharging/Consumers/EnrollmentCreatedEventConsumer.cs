using GymErp.Domain.Subscriptions.Aggreates.Enrollments;

namespace GymErp.Domain.Financial.Features.ProcessCharging.Consumers;

public class EnrollmentCreatedEventConsumer(Handler processChargingHandler)
{
    public async Task HandleAsync(EnrollmentCreatedEvent message, CancellationToken cancellationToken)
    {
        await processChargingHandler.HandleAsync(message, cancellationToken);
    }
}
