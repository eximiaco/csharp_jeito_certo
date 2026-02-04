namespace GymErp.Domain.Orchestration.Features.NewEnrollmentFlow;

public readonly record struct Request(
    Guid ClientId,
    Guid PlanId);