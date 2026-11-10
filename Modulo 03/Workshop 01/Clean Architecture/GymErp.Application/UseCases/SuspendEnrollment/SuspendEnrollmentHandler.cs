using CSharpFunctionalExtensions;
using GymErp.Application.Abstractions;

namespace GymErp.Application.UseCases.SuspendEnrollment;

public class SuspendEnrollmentHandler(
    IEnrollmentRepository repository,
    IUnitOfWork unitOfWork) : ISuspendEnrollmentUseCase
{
    public async Task<Result<bool, EnrollmentError>> HandleAsync(SuspendEnrollmentCommand request, CancellationToken cancellationToken = default)
    {
        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, cancellationToken);
        if (enrollment == null)
            return EnrollmentError.NotFound(request.EnrollmentId);

        var result = enrollment.Suspend(request.SuspensionStartDate, request.SuspensionEndDate);
        if (result.IsFailure)
            return EnrollmentError.RuleViolated(result.Error);

        await repository.UpdateAsync(enrollment, cancellationToken);
        await unitOfWork.Commit(cancellationToken);

        return true;
    }
}
