using GymErp.Common;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Infrastructure;

namespace GymErp.Domain.Subscriptions.Features.CancelEnrollment;

public class Handler(EnrollmentRepository repository, IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(CancelEnrollmentCommand command, CancellationToken cancellationToken)
    {
        var enrollment = await repository.GetByIdAsync(command.EnrollmentId, cancellationToken);
        if (enrollment == null)
            throw new InvalidOperationException("Inscrição não encontrada");

        var cancelResult = enrollment.Cancel();
        if (cancelResult.IsFailure)
            throw new InvalidOperationException(cancelResult.Error);

        await repository.UpdateAsync(enrollment, cancellationToken);
        await unitOfWork.Commit(cancellationToken);
    }
}