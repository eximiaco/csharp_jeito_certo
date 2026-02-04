namespace GymErp.Domain.Financial.Infrastructure.Gateways;

/// <summary>
/// Cliente da API do provedor de pagamento (camada sintática).
/// Normaliza a comunicação com o meio de pagamento, expondo apenas operações relevantes.
/// </summary>
public interface IPaymentProviderClient
{
    Task<ProviderChargeResponse> ChargeAsync(ProviderChargeRequest request, CancellationToken cancellationToken = default);
}
