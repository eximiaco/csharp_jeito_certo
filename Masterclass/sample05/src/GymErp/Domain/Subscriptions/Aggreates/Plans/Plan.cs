using GymErp.Common;
using CSharpFunctionalExtensions;

namespace GymErp.Domain.Subscriptions.Aggreates.Plans;

public sealed class Plan : Aggregate
{
    private Plan() { }

    private Plan(Guid id, string description, PlanType type)
    {
        Id = id;
        Description = description;
        Type = type;
    }

    public Guid Id { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public PlanType Type { get; private set; }

    public static Result<Plan> Create(string description, PlanType type)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Result.Failure<Plan>("Descrição não pode ser vazia");

        if (description.Length < 3)
            return Result.Failure<Plan>("Descrição deve ter pelo menos 3 caracteres");

        if (description.Length > 100)
            return Result.Failure<Plan>("Descrição deve ter no máximo 100 caracteres");

        return Result.Success(new Plan(Guid.NewGuid(), description.Trim(), type));
    }
}
