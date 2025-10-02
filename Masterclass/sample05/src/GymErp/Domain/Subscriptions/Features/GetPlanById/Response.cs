using GymErp.Domain.Subscriptions.Aggreates.Plans;

namespace GymErp.Domain.Subscriptions.Features.GetPlanById;

public record Response(Guid Id, string Description, PlanType Type);
