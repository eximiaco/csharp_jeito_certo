using CSharpFunctionalExtensions;
using GymErp.Common;
using System.Text.RegularExpressions;

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
        var validationResult = ValidateClient(client);
        if (validationResult.IsFailure)
            return Result.Failure<Enrollment>(validationResult.Error);

        return new Enrollment(Guid.NewGuid(), client, DateTime.UtcNow, EState.Suspended);
    }

    public static Result<Enrollment> Create(
        string name,
        string email,
        string phone,
        string document,
        DateTime birthDate,
        string gender,
        string address)
    {
        var client = new Client(document, name, email, phone, address);
        return Create(client);
    }

    private static Result ValidateClient(Client client)
    {
        if (string.IsNullOrWhiteSpace(client.Cpf))
            return Result.Failure("CPF não pode ser vazio");

        if (!IsValidCpf(client.Cpf))
            return Result.Failure("CPF inválido");

        if (string.IsNullOrWhiteSpace(client.Name))
            return Result.Failure("Nome não pode ser vazio");

        if (client.Name.Trim().Length < 10)
            return Result.Failure("Nome deve ter pelo menos 10 caracteres");

        if (string.IsNullOrWhiteSpace(client.Email))
            return Result.Failure("Email não pode ser vazio");

        if (!IsValidEmail(client.Email))
            return Result.Failure("Email inválido");

        if (string.IsNullOrWhiteSpace(client.Phone))
            return Result.Failure("Telefone não pode ser vazio");

        if (!IsValidPhone(client.Phone))
            return Result.Failure("Telefone inválido");

        return Result.Success();
    }

    private static bool IsValidCpf(string cpf)
    {
        // Remove caracteres não numéricos
        cpf = new string(cpf.Where(char.IsDigit).ToArray());

        if (cpf.Length != 11)
            return false;

        // Verifica se todos os dígitos são iguais
        if (cpf.Distinct().Count() == 1)
            return false;

        // Validação do primeiro dígito verificador
        var sum = 0;
        for (int i = 0; i < 9; i++)
            sum += int.Parse(cpf[i].ToString()) * (10 - i);

        var remainder = sum % 11;
        var digit1 = remainder < 2 ? 0 : 11 - remainder;

        if (digit1 != int.Parse(cpf[9].ToString()))
            return false;

        // Validação do segundo dígito verificador
        sum = 0;
        for (int i = 0; i < 10; i++)
            sum += int.Parse(cpf[i].ToString()) * (11 - i);

        remainder = sum % 11;
        var digit2 = remainder < 2 ? 0 : 11 - remainder;

        return digit2 == int.Parse(cpf[10].ToString());
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidPhone(string phone)
    {
        // Remove caracteres não numéricos
        phone = new string(phone.Where(char.IsDigit).ToArray());
        
        // Verifica se tem entre 10 e 11 dígitos (com DDD)
        return phone.Length >= 10 && phone.Length <= 11;
    }
}