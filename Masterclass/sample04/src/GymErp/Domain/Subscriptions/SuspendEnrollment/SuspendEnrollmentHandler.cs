using CSharpFunctionalExtensions;
using GymErp.Common;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Infrastructure;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace GymErp.Domain.Subscriptions.SuspendEnrollment;

public class Handler(EnrollmentRepository repository, IUnitOfWork unitOfWork, CancellationToken cancellationToken)
{
    public async Task<bool> HandleAsync(SuspendEnrollmentCommand request)
    {
        if (!request.IsValid())
            return false;

        var enrollment = await repository.GetByIdAsync(request.EnrollmentId, cancellationToken);
        if (enrollment == null)
            return false;

        var result = enrollment.Suspend(request.SuspensionStartDate, request.SuspensionEndDate);
        if (result.IsFailure)
            return false;

        await repository.UpdateAsync(enrollment, cancellationToken);
        await unitOfWork.Commit(cancellationToken);

        return true;
    }
} 