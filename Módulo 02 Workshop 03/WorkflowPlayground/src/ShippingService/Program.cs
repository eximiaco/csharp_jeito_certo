using Microsoft.AspNetCore.Mvc;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

var app = builder.Build();

app.MapPost("shippings", async (CreateShippingRequest request, ILogger<Program> logger) =>
{
    await Task.Delay(new Random().Next(2000, 5000));
    logger.LogInformation("Shipping scheduled for order {OrderId}", request.OrderId);

    return Results.Ok(new { ShippingId = Guid.NewGuid().ToString() });
});

app.MapPost("shippings/{shippingId}/cancel", async (
    [FromRoute] string shippingId,
    CancelShippingRequest request,
    ILogger<Program> logger) =>
{
    await Task.Delay(new Random().Next(2000, 5000));
    logger.LogWarning("Shipping {ShippingId} canceled for order {OrderId}", shippingId, request.OrderId);

    return Results.Ok(new { ShippingId = Guid.NewGuid().ToString() });
});

await app.RunAsync();

record CreateShippingRequest(string OrderId);
record CancelShippingRequest(string OrderId);
