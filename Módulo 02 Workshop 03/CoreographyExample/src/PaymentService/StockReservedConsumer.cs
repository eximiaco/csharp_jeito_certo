using OrderService.Common;

namespace PaymentService;

public class StockReservedConsumer(
    IServiceScopeFactory scopeFactory,
    ILogger<StockReservedConsumer> logger) : IHostedService
{
    private readonly QueueService _queueService = scopeFactory
        .CreateScope()
        .ServiceProvider
        .GetRequiredService<QueueService>();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _queueService.Subscribe<StockReservedEvent>(QueueNames.StockReservedQueue, async (order) =>
        {
            // Payment process logic
            logger.LogInformation("Payment processed for order {OrderId}", order.OrderId);
            await _queueService.PublishAsync(QueueNames.PaymentProcessedQueue, new PaymentProcessedEvent(order.OrderId));
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _queueService.Dispose();
        logger.LogInformation($"Stopping {nameof(StockReservedConsumer)}");
        return Task.FromResult(0);
    }
}
