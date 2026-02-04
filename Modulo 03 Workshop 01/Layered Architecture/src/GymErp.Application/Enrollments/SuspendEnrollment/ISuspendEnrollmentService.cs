using CSharpFunctionalExtensions;

namespace GymErp.Application.Enrollments.SuspendEnrollment;

public interface ISuspendEnrollmentService
{
    Task<Result<bool>> HandleAsync(SuspendEnrollmentCommand request, CancellationToken cancellationToken = default);
}
