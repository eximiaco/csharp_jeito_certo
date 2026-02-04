namespace GymErp.Domain.Financial.Infrastructure.Gateways;

/// <summary>
/// Implementação simulada do cliente do provedor (camada sintática).
/// Simula delay e retorno de sucesso para demonstração.
/// </summary>
public class FakePaymentProviderClient : IPaymentProviderClient
{
    public async Task<ProviderChargeResponse> ChargeAsync(ProviderChargeRequest request, CancellationToken cancellationToken = default)
    {
        await Task.Delay(150, cancellationToken);

        return new ProviderChargeResponse
        {
            Success = true,
            TransactionId = $"TXN-{Guid.NewGuid():N}"[..20],
            StatusCode = "00",
            Message = "Approved"
        };
    }
}
