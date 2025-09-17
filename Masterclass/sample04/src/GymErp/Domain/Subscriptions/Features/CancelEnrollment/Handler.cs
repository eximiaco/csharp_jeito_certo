using CSharpFunctionalExtensions;
using GymErp.Common;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;

namespace GymErp.Domain.Subscriptions.Features.CancelEnrollment;

public class Handler(EnrollmentRepository repository, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
{
    public async Task<Result> HandleAsync(Request request)
    {
        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, cancellationToken);
        if (enrollment == null)
            return Result.Failure("Inscrição não encontrada");

        var result = enrollment.Cancel();
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(enrollment, cancellationToken);
        await unitOfWork.Commit(cancellationToken);

        return Result.Success();
    }
} 