namespace GymErp.Domain.Subscriptions.Features.CancelEnrollment.Consumers;

public class CancelEnrollmentCommandSilverbackConsumer(Handler handler)
{
    public async Task HandleAsync(CancelEnrollmentCommand message, CancellationToken cancellationToken)
    {
        await handler.HandleAsync(message, cancellationToken);
    }
}
