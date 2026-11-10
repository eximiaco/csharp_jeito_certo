using CSharpFunctionalExtensions;

namespace GymErp.Core.Ports.Driving;

// Porta de condução: uma conversa com propósito ("gerenciar matrículas"), não uma interface por classe.
// Quem conduz pode ser HTTP, um consumer, uma CLI ou um teste: o núcleo não sabe nem precisa saber.
public interface IForManagingEnrollments
{
    Task<Result<Guid, EnrollmentError>> EnrollAsync(EnrollRequest request, CancellationToken cancellationToken = default);
    Task<Result<CancelResponse, EnrollmentError>> CancelAsync(CancelRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool, EnrollmentError>> SuspendAsync(SuspendRequest request, CancellationToken cancellationToken = default);
}

// O que cruza a porta são estruturas de dados simples, nunca a entidade.
public sealed record EnrollRequest(string Name, string Email, string Phone, string Document, string Address);
public sealed record CancelRequest(Guid EnrollmentId, string Reason);
public sealed record CancelResponse(Guid EnrollmentId, DateTime CanceledAt);
public sealed record SuspendRequest(Guid EnrollmentId, DateTime SuspensionStartDate, DateTime SuspensionEndDate);

public enum EnrollmentErrorKind
{
    NotFound,
    RuleViolated
}

public sealed record EnrollmentError(EnrollmentErrorKind Kind, string Message)
{
    public static EnrollmentError NotFound(Guid enrollmentId) =>
        new(EnrollmentErrorKind.NotFound, $"Inscrição {enrollmentId} não encontrada");

    public static EnrollmentError RuleViolated(string message) =>
        new(EnrollmentErrorKind.RuleViolated, message);
}
