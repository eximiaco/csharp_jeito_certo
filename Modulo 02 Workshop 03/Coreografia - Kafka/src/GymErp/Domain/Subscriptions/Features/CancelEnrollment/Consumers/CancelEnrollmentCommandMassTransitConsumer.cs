using MassTransit;

namespace GymErp.Domain.Subscriptions.Features.CancelEnrollment.Consumers;

public class CancelEnrollmentCommandMassTransitConsumer(Handler handler) : IConsumer<CancelEnrollmentCommand>
{
    public async Task Consume(ConsumeContext<CancelEnrollmentCommand> context)
    {
        await handler.HandleAsync(context.Message, context.CancellationToken);
    }
}
