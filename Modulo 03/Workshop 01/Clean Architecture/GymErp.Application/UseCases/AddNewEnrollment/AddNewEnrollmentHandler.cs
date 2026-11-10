using CSharpFunctionalExtensions;
using GymErp.Application.Abstractions;
using GymErp.Domain.Enrollments;

namespace GymErp.Application.UseCases.AddNewEnrollment;

public class AddNewEnrollmentHandler(
    IEnrollmentRepository repository,
    IUnitOfWork unitOfWork) : IAddNewEnrollmentUseCase
{
    public async Task<Result<Guid, EnrollmentError>> HandleAsync(AddNewEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var enrollmentResult = Enrollment.Create(
            request.Name,
            request.Email,
            request.Phone,
            request.Document,
            request.Address);

        if (enrollmentResult.IsFailure)
            return EnrollmentError.RuleViolated(enrollmentResult.Error);

        var enrollment = enrollmentResult.Value;
        await repository.AddAsync(enrollment, cancellationToken);
        await unitOfWork.Commit(cancellationToken);

        return enrollment.Id;
    }
}
