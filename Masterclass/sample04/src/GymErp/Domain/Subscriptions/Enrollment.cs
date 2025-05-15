using CSharpFunctionalExtensions;
using GymErp.Common;

namespace GymErp.Domain.Subscriptions;

public record Client(string Cpf, string Name, string Email, string Phone, string Address);

public enum EState
{
    Active,
    Suspended,
    Canceled
}
public sealed class Enrollment : IAggregate
{
    private Enrollment(Guid id, Client client, DateTime requestDate, EState state)
    {
        Id = id;
        Client = client;
        RequestDate = requestDate;
        State = state;
    }
    
    public Guid Id { get; }
    public Client Client { get; }
    public DateTime RequestDate { get; }
    public EState State { get; }

    public static Result<Enrollment> Create(Client client)
    {
        return new Enrollment(Guid.NewGuid(), client, DateTime.UtcNow, EState.Suspended);
    }
}