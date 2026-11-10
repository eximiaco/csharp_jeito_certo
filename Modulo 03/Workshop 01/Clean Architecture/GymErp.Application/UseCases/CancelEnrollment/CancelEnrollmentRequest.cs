namespace GymErp.Application.UseCases.CancelEnrollment;

public record CancelEnrollmentRequest
{
    public Guid EnrollmentId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
