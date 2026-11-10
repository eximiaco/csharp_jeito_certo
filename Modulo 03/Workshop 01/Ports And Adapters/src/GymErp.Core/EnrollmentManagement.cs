using CSharpFunctionalExtensions;
using GymErp.Core.Ports.Driven;
using GymErp.Core.Ports.Driving;
using GymErp.Domain.Enrollments;

namespace GymErp.Core;

// Implementação da porta de condução. Orquestra o domínio e depende só de portas conduzidas.
public sealed class EnrollmentManagement(IForStoringEnrollments enrollments) : IForManagingEnrollments
{
    public async Task<Result<Guid, EnrollmentError>> EnrollAsync(EnrollRequest request, CancellationToken cancellationToken = default)
    {
        var created = Enrollment.Create(request.Name, request.Email, request.Phone, request.Document, request.Address);
        if (created.IsFailure)
            return EnrollmentError.RuleViolated(created.Error);

        await enrollments.SaveAsync(created.Value, cancellationToken);
        return created.Value.Id;
    }

    public async Task<Result<CancelResponse, EnrollmentError>> CancelAsync(CancelRequest request, CancellationToken cancellationToken = default)
    {
        var enrollment = await enrollments.FindAsync(request.EnrollmentId, cancellationToken);
        if (enrollment is null)
            return EnrollmentError.NotFound(request.EnrollmentId);

        var canceled = enrollment.Cancel(request.Reason);
        if (canceled.IsFailure)
            return EnrollmentError.RuleViolated(canceled.Error);

        await enrollments.SaveAsync(enrollment, cancellationToken);
        return new CancelResponse(enrollment.Id, DateTime.UtcNow);
    }

    public async Task<Result<bool, EnrollmentError>> SuspendAsync(SuspendRequest request, CancellationToken cancellationToken = default)
    {
        var enrollment = await enrollments.FindAsync(request.EnrollmentId, cancellationToken);
        if (enrollment is null)
            return EnrollmentError.NotFound(request.EnrollmentId);

        // A regra "suspensão mínima de 30 dias" mora só na entidade.
        var suspended = enrollment.Suspend(request.SuspensionStartDate, request.SuspensionEndDate);
        if (suspended.IsFailure)
            return EnrollmentError.RuleViolated(suspended.Error);

        await enrollments.SaveAsync(enrollment, cancellationToken);
        return true;
    }
}
