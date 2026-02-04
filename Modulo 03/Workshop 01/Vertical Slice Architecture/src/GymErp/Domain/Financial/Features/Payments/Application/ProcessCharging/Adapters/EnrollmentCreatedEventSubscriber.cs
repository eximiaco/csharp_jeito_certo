using GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging;
using GymErp.Domain.Subscriptions.Features.Enrollments.Domain;
using Microsoft.Extensions.Logging;

namespace GymErp.Domain.Financial.Features.Payments.Application.ProcessCharging.Adapters;

public class EnrollmentCreatedEventSubscriber(Handler handler, ILogger<EnrollmentCreatedEventSubscriber> logger)
{
    private const decimal DefaultChargeAmount = 99.90m;
    private const string DefaultCurrency = "BRL";

    public async Task HandleAsync(EnrollmentCreatedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Processing charging for enrollment {EnrollmentId}", message.EnrollmentId);

        var command = new ProcessChargingCommand(message.EnrollmentId, DefaultChargeAmount, DefaultCurrency);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Failed to create payment for enrollment {EnrollmentId}: {Error}", message.EnrollmentId, result.Error);
            return;
        }

        logger.LogInformation("Charging processed successfully for enrollment {EnrollmentId}, payment {PaymentId}", message.EnrollmentId, result.Value.PaymentId);
    }
}
