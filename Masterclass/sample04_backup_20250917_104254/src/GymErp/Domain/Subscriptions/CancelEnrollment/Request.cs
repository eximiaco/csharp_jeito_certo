namespace GymErp.Domain.Subscriptions.CancelEnrollment;

public record Request
{
    public Guid EnrollmentId { get; init; }
    public string Reason { get; init; } = string.Empty;
} 