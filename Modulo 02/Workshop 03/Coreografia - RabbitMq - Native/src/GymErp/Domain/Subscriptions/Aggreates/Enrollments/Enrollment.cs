using CSharpFunctionalExtensions;
using GymErp.Common;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments.States;

namespace GymErp.Domain.Subscriptions.Aggreates.Enrollments;

public sealed class Enrollment : Aggregate
{
    private Enrollment() { }

    private Enrollment(Guid id, Client client, DateTime requestDate, EState state)
    {
        Id = id;
        Client = client;
        RequestDate = requestDate;
        State = state;
        _state = EnrollmentStateFactory.CreateState(state);
    }
    
    public Guid Id { get; private set; }
    public Client Client { get; private set; } = null!;
    public DateTime RequestDate { get; private set; }
    public EState State { get; private set; }
    public DateTime? SuspensionStartDate { get; private set; }
    public DateTime? SuspensionEndDate { get; private set; }
    private IEnrollmentState _state = null!;

    public static Result<Enrollment> Create(Client client)
    {
        var validationResult = ValidateClient(client);
        if (validationResult.IsFailure)
            return Result.Failure<Enrollment>(validationResult.Error);
        var enrollment = new Enrollment(Guid.NewGuid(), client, DateTime.UtcNow, EState.Suspended);
        enrollment.AddDomainEvent(new EnrollmentCreatedEvent(enrollment.Id));
        return enrollment;
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

    public Result Activate()
    {
        return _state.Activate(this);
    }

    public Result Suspend(DateTime startDate, DateTime endDate)
    {
        if (startDate >= endDate)
            return Result.Failure("A data de início da suspensão deve ser anterior à data de término");

        var minimumSuspensionPeriod = TimeSpan.FromDays(30);
        if (endDate - startDate < minimumSuspensionPeriod)
            return Result.Failure("O período mínimo de suspensão é de 30 dias");

        var result = _state.Suspend(this);
        if (result.IsSuccess)
        {
            SuspensionStartDate = startDate;
            SuspensionEndDate = endDate;
        }

        return result;
    }

    public Result Cancel()
    {
        var result = _state.Cancel(this);
        if (result.IsSuccess)
        {
            AddDomainEvent(new EnrollmentCanceledEvent(Id, DateTime.UtcNow));
        }
        return result;
    }

    internal void ChangeState(EState newState)
    {
        _state = EnrollmentStateFactory.CreateState(newState);
        State = newState;
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