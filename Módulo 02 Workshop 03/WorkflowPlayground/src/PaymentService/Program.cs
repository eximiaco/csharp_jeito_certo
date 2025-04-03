using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

var app = builder.Build();

app.MapPost("payments", async (CreatePaymentRequest request, ILogger<Program> logger) =>
{
    await Task.Delay(new Random().Next(2000, 5000));
    logger.LogInformation("Payment created for order {OrderId}", request.OrderId);

    return Results.Ok(new { PaymentId = Guid.NewGuid().ToString() });
});

app.MapPost("payments/{paymentId}/refund", async (
    [FromRoute] string paymentId,
    RefundPaymentRequest request,
    ILogger<Program> logger) =>
{
    await Task.Delay(new Random().Next(2000, 5000));
    logger.LogWarning("Payment {PaymentId} refunded for order {OrderId}", paymentId, request.OrderId);

    return Results.Ok(new { PaymentId = Guid.NewGuid().ToString() });
});

await app.RunAsync();

record CreatePaymentRequest(string OrderId);
record RefundPaymentRequest(string OrderId);