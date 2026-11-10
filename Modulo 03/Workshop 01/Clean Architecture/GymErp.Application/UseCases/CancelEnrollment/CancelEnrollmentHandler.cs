using CSharpFunctionalExtensions;
using GymErp.Application.Abstractions;

namespace GymErp.Application.UseCases.CancelEnrollment;

public class CancelEnrollmentHandler(
    IEnrollmentRepository repository,
    IUnitOfWork unitOfWork) : ICancelEnrollmentUseCase
{
    public async Task<Result<CancelEnrollmentResponse, EnrollmentError>> HandleAsync(CancelEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, cancellationToken);
        if (enrollment == null)
            return EnrollmentError.NotFound(request.EnrollmentId);

        var cancelResult = enrollment.Cancel(request.Reason);
        if (cancelResult.IsFailure)
            return EnrollmentError.RuleViolated(cancelResult.Error);

        await repository.UpdateAsync(enrollment, cancellationToken);
        await unitOfWork.Commit(cancellationToken);

        // A resposta é uma estrutura simples: a entidade não cruza a fronteira.
        return new CancelEnrollmentResponse(enrollment.Id, DateTime.UtcNow);
    }
}
