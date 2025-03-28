using OrderService.Common;
using RabbitMQ.Client;
using Serilog;
using StockService;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.AddHostedService<OrderCreatedConsumer>();
builder.Services.AddHostedService<PaymentCanceledConsumer>();
builder.Services.AddSingleton((_) =>
{
    return new ConnectionFactory { HostName = "localhost" }.CreateConnectionAsync().Result;
});

builder.Services.AddScoped<QueueService>();

var app = builder.Build();
await app.RunAsync();