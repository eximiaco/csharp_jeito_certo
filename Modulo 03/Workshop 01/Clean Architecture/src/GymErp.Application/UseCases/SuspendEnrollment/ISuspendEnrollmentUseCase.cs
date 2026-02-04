using CSharpFunctionalExtensions;

namespace GymErp.Application.UseCases.SuspendEnrollment;

public interface ISuspendEnrollmentUseCase
{
    Task<Result<bool>> HandleAsync(SuspendEnrollmentCommand request, CancellationToken cancellationToken = default);
}
