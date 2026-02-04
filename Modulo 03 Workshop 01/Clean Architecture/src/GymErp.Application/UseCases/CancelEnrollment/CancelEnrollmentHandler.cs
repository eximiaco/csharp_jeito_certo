using CSharpFunctionalExtensions;
using GymErp.Application.Abstractions;

namespace GymErp.Application.UseCases.CancelEnrollment;

public class CancelEnrollmentHandler(
    IEnrollmentRepository repository,
    IUnitOfWork unitOfWork) : ICancelEnrollmentUseCase
{
    public async Task<Result<CancelEnrollmentResponse>> HandleAsync(CancelEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, cancellationToken);
        if (enrollment == null)
            return Result.Failure<CancelEnrollmentResponse>("Inscrição não encontrada");

        var cancelResult = enrollment.Cancel();
        if (cancelResult.IsFailure)
            return Result.Failure<CancelEnrollmentResponse>(cancelResult.Error);

        await repository.UpdateAsync(enrollment, cancellationToken);
        await unitOfWork.Commit(cancellationToken);

        return Result.Success(new CancelEnrollmentResponse(enrollment.Id, DateTime.UtcNow));
    }
}
