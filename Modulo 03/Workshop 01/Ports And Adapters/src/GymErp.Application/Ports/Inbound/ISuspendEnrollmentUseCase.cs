using CSharpFunctionalExtensions;
using GymErp.Application.UseCases.SuspendEnrollment;

namespace GymErp.Application.Ports.Inbound;

public interface ISuspendEnrollmentUseCase
{
    Task<Result<bool>> HandleAsync(SuspendEnrollmentCommand request, CancellationToken cancellationToken = default);
}
