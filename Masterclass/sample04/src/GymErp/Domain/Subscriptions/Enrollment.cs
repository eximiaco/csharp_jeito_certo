using GymErp.Common;

namespace GymErp.Domain.Subscriptions;

public record Client(string Cpf, string Name, string Email, string Phone, string Address);


public record Plan(Guid Id, string Name);

public sealed class Enrollment : IAggregate
{
    
}