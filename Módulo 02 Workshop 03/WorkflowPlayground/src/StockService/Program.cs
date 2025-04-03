using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

var app = builder.Build();

app.MapPost("stock", async (ReserveStockRequest request, ILogger<Program> logger) =>
{
    await Task.Delay(new Random().Next(2000, 5000));
    logger.LogInformation("Stock reserved for order {OrderId}", request.OrderId);

    return Results.Ok(new { ReservationId = Guid.NewGuid().ToString() });
});

app.MapPost("stock/{reservationId}/cancel", async (
    [FromRoute] string reservationId,
    CancelStockReservationRequest request,
    ILogger<Program> logger) =>
{
    await Task.Delay(new Random().Next(2000, 5000));
    logger.LogWarning("Stock reservation {ReservationId} canceled for {OrderId}", reservationId,request.OrderId);

    return Results.Ok(new { ReservationId = Guid.NewGuid().ToString() });
});


await app.RunAsync();

record ReserveStockRequest(string OrderId);
record CancelStockReservationRequest(string OrderId);