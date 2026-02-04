namespace GymErp.Domain.Subscriptions.Features.CancelEnrollment;

public record CancelEnrollmentCommand(Guid EnrollmentId, string Reason);
