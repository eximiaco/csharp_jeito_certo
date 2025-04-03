using System.Text;
using System.Text.Json;
using OrderService.Domain;

namespace OrderService.Infra.Services;

public class StockService(HttpClient httpClient)
{
    private sealed record StockReservationResponse(string ReservationId);

    public async Task<string> ReserveAsync(Order order)
    {
        var response = await httpClient.PostAsJsonAsync(
            requestUri: "stock",
            value: new { OrderId = order.Id });

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadFromJsonAsync<StockReservationResponse>();
        return responseContent!.ReservationId;
    }

    public async Task UndoStockReservationAsync(Order order)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(order.StockReservationId);

        var response = await httpClient.PostAsJsonAsync(
            requestUri: $"stock/{order.StockReservationId}/cancel",
            value: new { OrderId = order.Id });

        response.EnsureSuccessStatusCode();
    }
}