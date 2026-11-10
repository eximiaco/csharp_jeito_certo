using CSharpFunctionalExtensions;

namespace GymErp.Application.UseCases.SuspendEnrollment;

public interface ISuspendEnrollmentUseCase
{
    Task<Result<bool, EnrollmentError>> HandleAsync(SuspendEnrollmentCommand request, CancellationToken cancellationToken = default);
}
