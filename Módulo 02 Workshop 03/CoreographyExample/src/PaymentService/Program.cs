using OrderService.Common;
using PaymentService;
using RabbitMQ.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.AddHostedService<StockReservedConsumer>();
builder.Services.AddHostedService<ShippingCanceledConsumer>();

builder.Services.AddSingleton((_) =>
{
    return new ConnectionFactory { HostName = "localhost" }.CreateConnectionAsync().Result;
});

builder.Services.AddScoped<QueueService>();

var app = builder.Build();
app.Run();