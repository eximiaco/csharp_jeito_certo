using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using MassTransit;

namespace GymErp.Domain.Financial.Features.ProcessCharging.Consumers;

public class EnrollmentCreatedEventMassTransitConsumer(Handler processChargingHandler) : IConsumer<EnrollmentCreatedEvent>
{
    public async Task Consume(ConsumeContext<EnrollmentCreatedEvent> context)
    {
        await processChargingHandler.HandleAsync(context.Message, context.CancellationToken);
    }
}
