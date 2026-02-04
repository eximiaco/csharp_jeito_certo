using CSharpFunctionalExtensions;

namespace GymErp.Application.Enrollments.AddNewEnrollment;

public interface IAddNewEnrollmentService
{
    Task<Result<Guid>> HandleAsync(AddNewEnrollmentRequest request, CancellationToken cancellationToken = default);
}
