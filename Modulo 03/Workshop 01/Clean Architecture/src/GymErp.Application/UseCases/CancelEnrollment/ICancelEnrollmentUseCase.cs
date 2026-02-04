using CSharpFunctionalExtensions;

namespace GymErp.Application.UseCases.CancelEnrollment;

public interface ICancelEnrollmentUseCase
{
    Task<Result<CancelEnrollmentResponse>> HandleAsync(CancelEnrollmentRequest request, CancellationToken cancellationToken = default);
}
