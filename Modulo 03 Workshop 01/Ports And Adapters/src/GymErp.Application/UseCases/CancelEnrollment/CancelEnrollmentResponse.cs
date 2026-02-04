namespace GymErp.Application.UseCases.CancelEnrollment;

public record CancelEnrollmentResponse(Guid EnrollmentId, DateTime CanceledAt);
