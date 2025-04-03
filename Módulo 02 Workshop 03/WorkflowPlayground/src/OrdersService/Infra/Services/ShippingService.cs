using System.Text;
using System.Text.Json;
using OrderService.Domain;

namespace OrderService.Infra.Services;

public class ShippingService(HttpClient httpClient)
{
    private sealed record ShippingResponse(string ShippingId);

    public async Task<string> ShipAsync(Order order)
    {
        var response = await httpClient.PostAsJsonAsync(
            requestUri: "shippings",
            value: new { OrderId = order.Id }
        );

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadFromJsonAsync<ShippingResponse>();
        return responseContent!.ShippingId;
    }

    public async Task CancelShippingAsync(Order order)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(order.ShippingId);

        var response = await httpClient.PostAsJsonAsync(
            requestUri: $"shipping/{order.ShippingId}/cancel",
            value: new { OrderId = order.Id }
        );

        response.EnsureSuccessStatusCode();
    }
}