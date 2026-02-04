using CSharpFunctionalExtensions;
using GymErp.Application.Common;
using GymErp.Domain.Enrollments;

namespace GymErp.Application.Enrollments.AddNewEnrollment;

public class AddNewEnrollmentHandler(
    IEnrollmentRepository repository,
    IUnitOfWork unitOfWork) : IAddNewEnrollmentService
{
    public async Task<Result<Guid>> HandleAsync(AddNewEnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var enrollmentResult = Enrollment.Create(
            request.Name,
            request.Email,
            request.Phone,
            request.Document,
            request.BirthDate,
            request.Gender,
            request.Address
        );

        if (enrollmentResult.IsFailure)
            return Result.Failure<Guid>(enrollmentResult.Error);

        var enrollment = enrollmentResult.Value;
        await repository.AddAsync(enrollment, cancellationToken);
        await unitOfWork.Commit(cancellationToken);

        return Result.Success(enrollment.Id);
    }
}
