namespace GymErp.Application.Enrollments.CancelEnrollment;

public record CancelEnrollmentResponse(Guid EnrollmentId, DateTime CanceledAt);
