namespace GymErp.Domain.Subscriptions.Features.SuspendEnrollment;

public record SuspendEnrollmentCommand(
    Guid EnrollmentId,
    DateTime SuspensionStartDate,
    DateTime SuspensionEndDate)
{
    public bool IsValid()
    {
        if (SuspensionStartDate >= SuspensionEndDate)
            return false;

        var minimumSuspensionPeriod = TimeSpan.FromDays(30);
        if (SuspensionEndDate - SuspensionStartDate < minimumSuspensionPeriod)
            return false;

        return true;
    }
} 