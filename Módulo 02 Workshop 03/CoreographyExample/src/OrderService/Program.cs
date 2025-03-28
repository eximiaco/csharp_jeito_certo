using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using OrderService.BackgroundServices;
using OrderService.Common;
using OrderService.Consumers;
using OrderService.Data;
using RabbitMQ.Client;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.AddSerilog();

builder.Services.AddHostedService<ShippingScheduledConsumer>();
builder.Services.AddHostedService<StockReservationCanceledConsumer>();
builder.Services.AddHostedService<OutboxMessageChannelConsumer>();
builder.Services.AddHostedService<OutboxBackgroundFallbackService>();

builder.Services.AddSingleton(_ => Channel.CreateUnbounded<OutboxMessage>());

builder.Services.AddSingleton((_) =>
{
    return new ConnectionFactory { HostName = "localhost" }.CreateConnectionAsync().Result;
});

builder.Services.AddScoped<QueueService>();
builder.Services.AddDbContext<OrderDbContext>(opts => opts.UseSqlServer(builder.Configuration.GetConnectionString("Database")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapGet("/order", async (
    OrderDbContext context,
    QueueService queueService,
    ILogger<Program> logger,
    Channel<OutboxMessage> channel,
    CancellationToken ct) =>
{
    // Order creation logic
    var order = new Order();

    var outboxMessage = new OutboxMessage(QueueNames.OrderCreatedQueue, new OrderCreatedEvent(order.Id));

    context.Orders.Add(order);
    context.OutboxMessages.Add(outboxMessage);

    await context.SaveChangesAsync(ct);
    await channel.Writer.WriteAsync(outboxMessage, ct);

    logger.LogInformation("Order {OrderId} created", order.Id);
    return Results.Ok($"Order {order.Id} created");
})
.WithName("CreateOrder");

using var scope = app.Services.CreateScope();
await scope.ServiceProvider.GetRequiredService<OrderDbContext>().Database.MigrateAsync();

await app.RunAsync();
