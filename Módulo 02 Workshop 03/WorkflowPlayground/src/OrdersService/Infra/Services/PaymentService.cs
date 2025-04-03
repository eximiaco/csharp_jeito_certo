using OrderService.Domain;

namespace OrderService.Infra.Services;

public class PaymentService(HttpClient httpClient)
{
    private sealed record PaymentResponse(string PaymentId);

    public async Task<string> MakePaymentAsync(Order order)
    {
        var response = await httpClient.PostAsJsonAsync(
            requestUri: "payments",
            value: new { OrderId = order.Id }
        );

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadFromJsonAsync<PaymentResponse>();
        return responseContent!.PaymentId;
    }

    public async Task RefundPaymentAsync(Order order)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(order.PaymentId);

        var response = await httpClient.PostAsJsonAsync(
            requestUri: $"payments/{order.PaymentId}/refund",
            value: new { OrderId = order.Id }
        );

        response.EnsureSuccessStatusCode();

        await response.Content.ReadFromJsonAsync<PaymentResponse>();
    }
}