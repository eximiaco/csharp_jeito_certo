using CSharpFunctionalExtensions;

namespace GymErp.Application.Enrollments.CancelEnrollment;

public interface ICancelEnrollmentService
{
    Task<Result<CancelEnrollmentResponse>> HandleAsync(CancelEnrollmentRequest request, CancellationToken cancellationToken = default);
}
