namespace GymErp.Domain.Subscriptions.Features.Enrollments.Application.CancelEnrollment;

public record Request
{
    public Guid EnrollmentId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
