namespace GymErp.Domain.Financial.Infrastructure.Gateways;

/// <summary>
/// Contrato sintático da requisição de cobrança na API do provedor de pagamento.
/// Normaliza o formato esperado pelo meio de pagamento (camada sintática).
/// </summary>
public record ProviderChargeRequest
{
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public string? Description { get; init; }
}
