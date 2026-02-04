namespace GymErp.Domain.Financial.Features.Payments.Services;

/// <summary>
/// Resultado de cobrança com significado no domínio da empresa (camada semântica).
/// Traduz a resposta do provedor para conceitos relevantes ao negócio.
/// </summary>
public record ChargeResult
{
    public bool Success { get; init; }
    public string? TransactionId { get; init; }
    public string? ErrorMessage { get; init; }

    public static ChargeResult Succeeded(string transactionId) => new()
    {
        Success = true,
        TransactionId = transactionId
    };

    public static ChargeResult Failed(string message) => new()
    {
        Success = false,
        ErrorMessage = message
    };
}
