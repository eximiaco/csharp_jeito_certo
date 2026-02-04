using CSharpFunctionalExtensions;
using GymErp.Common;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;

namespace GymErp.Domain.Subscriptions.Features.AddNewEnrollment;

public class Handler(EnrollmentRepository repository, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
{
    public async Task<Result<Guid>> HandleAsync(Request request)
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