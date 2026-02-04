namespace GymErp.Domain.Subscriptions.Features.CancelEnrollment;

public record Response(Guid EnrollmentId, DateTime CanceledAt);
