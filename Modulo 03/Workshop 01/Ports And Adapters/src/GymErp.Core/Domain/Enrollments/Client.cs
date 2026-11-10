namespace GymErp.Domain.Enrollments;

public record Client
{
    public string Cpf { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;

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
