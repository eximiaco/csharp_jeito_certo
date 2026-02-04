namespace GymErp.Application.Enrollments.CancelEnrollment;

public record CancelEnrollmentRequest
{
    public Guid EnrollmentId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
