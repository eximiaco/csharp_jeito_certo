namespace GymErp.Domain.Subscriptions.Features.Enrollments.Application.CancelEnrollment;

public record Response(Guid EnrollmentId, DateTime CanceledAt);
