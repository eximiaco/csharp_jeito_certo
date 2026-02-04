namespace GymErp.Domain.Financial.Infrastructure.Gateways;

/// <summary>
/// Resposta bruta da API do provedor de pagamento (camada sintática).
/// Preserva as características originais da API (códigos, mensagens do gateway).
/// </summary>
public record ProviderChargeResponse
{
    public bool Success { get; init; }
    public string? TransactionId { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}
