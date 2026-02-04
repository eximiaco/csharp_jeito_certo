using CSharpFunctionalExtensions;
using GymErp.Application.UseCases.CancelEnrollment;

namespace GymErp.Application.Ports.Inbound;

public interface ICancelEnrollmentUseCase
{
    Task<Result<CancelEnrollmentResponse>> HandleAsync(CancelEnrollmentRequest request, CancellationToken cancellationToken = default);
}
