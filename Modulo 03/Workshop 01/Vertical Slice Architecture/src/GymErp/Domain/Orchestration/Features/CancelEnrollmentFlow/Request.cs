namespace GymErp.Domain.Orchestration.Features.CancelEnrollmentFlow;

public record Request(Guid EnrollmentId, string Reason);
