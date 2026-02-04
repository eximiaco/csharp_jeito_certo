using GymErp.Domain.Financial.Infrastructure.Gateways;

namespace GymErp.Domain.Financial.Features.Payments.Services;

/// <summary>
/// Implementação da camada semântica: adapta a resposta sintática do provedor
/// para o domínio da empresa (ChargeResult), formalizando relações e significado.
/// Ref.: https://ontologia.eximia.co/conceitos/camada-semantica/
/// </summary>
public class PaymentChargeSemanticService(IPaymentProviderClient providerClient)
{
    public async Task<ChargeResult> ChargeAsync(decimal amount, string currency, string reference, string? description = null, CancellationToken cancellationToken = default)
    {
        var syntacticRequest = new ProviderChargeRequest
        {
            Amount = amount,
            Currency = currency,
            Reference = reference,
            Description = description
        };

        ProviderChargeResponse syntacticResponse = await providerClient.ChargeAsync(syntacticRequest, cancellationToken);

        return MapToSemanticResult(syntacticResponse);
    }

    private static ChargeResult MapToSemanticResult(ProviderChargeResponse response)
    {
        if (response.Success && !string.IsNullOrEmpty(response.TransactionId))
            return ChargeResult.Succeeded(response.TransactionId);

        var message = string.IsNullOrWhiteSpace(response.Message)
            ? "Cobrança recusada pelo provedor"
            : response.Message;

        return ChargeResult.Failed(message);
    }
}
