using CSharpFunctionalExtensions;
using GymErp.Common;

namespace GymErp.Domain.Subscriptions;

public record Client
{
    public string Cpf { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public Client() { }

    public Client(string cpf, string name, string email, string phone, string address)
    {
        Cpf = cpf;
        Name = name;
        Email = email;
        Phone = phone;
        Address = address;
    }
}

public enum EState
{
    Active,
    Suspended,
    Canceled
}

public sealed class Enrollment : IAggregate
{
    private Enrollment() { }

    private Enrollment(Guid id, Client client, DateTime requestDate, EState state)
    {
        Id = id;
        Client = client;
        RequestDate = requestDate;
        State = state;
    }
    
    public Guid Id { get; private set; }
    public Client Client { get; private set; } = null!;
    public DateTime RequestDate { get; private set; }
    public EState State { get; private set; }

    public static Result<Enrollment> Create(Client client)
    {
        return new Enrollment(Guid.NewGuid(), client, DateTime.UtcNow, EState.Suspended);
    }

    public static Enrollment Create(
        string name,
        string email,
        string phone,
        string document,
        DateTime birthDate,
        string gender,
        string address)
    {
        var client = new Client(document, name, email, phone, address);
        return new Enrollment(Guid.NewGuid(), client, DateTime.UtcNow, EState.Suspended);
    }
}