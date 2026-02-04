using CSharpFunctionalExtensions;
using GymErp.Application.UseCases.AddNewEnrollment;

namespace GymErp.Application.Ports.Inbound;

public interface IAddNewEnrollmentUseCase
{
    Task<Result<Guid>> HandleAsync(AddNewEnrollmentRequest request, CancellationToken cancellationToken = default);
}
