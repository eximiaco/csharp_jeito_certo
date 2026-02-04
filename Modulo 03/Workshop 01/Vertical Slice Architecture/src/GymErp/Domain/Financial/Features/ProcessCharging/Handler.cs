using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace GymErp.Domain.Financial.Features.ProcessCharging;

public class Handler(ILogger<Handler> logger)
{
    public async Task HandleAsync(EnrollmentCreatedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Hello World! Processing charging for enrollment {EnrollmentId}", message.EnrollmentId);
        
        // Simular processamento assíncrono
        await Task.Delay(100, cancellationToken);
        
        logger.LogInformation("Charging processed successfully for enrollment {EnrollmentId}", message.EnrollmentId);
    }
}

