namespace GymErp.Domain.Subscriptions.Aggreates.Enrollments;

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