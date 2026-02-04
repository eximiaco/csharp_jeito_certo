using CSharpFunctionalExtensions;

namespace GymErp.Application.UseCases.AddNewEnrollment;

public interface IAddNewEnrollmentUseCase
{
    Task<Result<Guid>> HandleAsync(AddNewEnrollmentRequest request, CancellationToken cancellationToken = default);
}
