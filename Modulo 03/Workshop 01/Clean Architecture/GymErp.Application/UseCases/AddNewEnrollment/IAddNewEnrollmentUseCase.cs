using CSharpFunctionalExtensions;

namespace GymErp.Application.UseCases.AddNewEnrollment;

public interface IAddNewEnrollmentUseCase
{
    Task<Result<Guid, EnrollmentError>> HandleAsync(AddNewEnrollmentRequest request, CancellationToken cancellationToken = default);
}
