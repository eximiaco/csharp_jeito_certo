using GymErp.Common;
using GymErp.Domain.Subscriptions;
using Gymerp.Domain.Subscriptions.Infrastructure;

namespace Gymerp.Domain.Subscriptions.AddNewEnrollment;

public class Handler(EnrollmentRepository enrollmentRepository, IUnitOfWork unitOfWork)
{
    public async Task<Guid> HandleAsync(Request request, CancellationToken cancellationToken)
    {
        var enrollment = Enrollment.Create(
            request.Name,
            request.Email,
            request.Phone,
            request.Document,
            request.BirthDate,
            request.Gender,
            request.Address
        );

        await enrollmentRepository.AddAsync(enrollment, cancellationToken);
        
        unitOfWork.Commit();

        return enrollment.Id;
    }
} 