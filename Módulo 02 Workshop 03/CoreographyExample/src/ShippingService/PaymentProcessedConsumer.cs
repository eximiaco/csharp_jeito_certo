using OrderService.Common;

namespace ShippingService;

public class PaymentProcessedConsumer(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentProcessedConsumer> logger) : IHostedService
{
    private readonly QueueService _queueService = scopeFactory
        .CreateScope()
        .ServiceProvider
        .GetRequiredService<QueueService>();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _queueService.Subscribe<PaymentProcessedEvent>(QueueNames.PaymentProcessedQueue, async (order) =>
        {
            try
            {
                //throw new Exception();
                logger.LogInformation("Shipping scheduled for order {OrderId}", order.OrderId);
                await _queueService.PublishAsync(QueueNames.ShippingScheduledQueue, new ShippingScheduledEvent(order.OrderId));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unrecovered error occurred, canceling shipping for order {OrderId}...", order.OrderId);
                await _queueService.PublishAsync(QueueNames.UndoShippingSchedulingQueue, new UndoShippingSchedulingEvent(order.OrderId));
            }
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _queueService.Dispose();
        logger.LogInformation($"Stopping {nameof(PaymentProcessedConsumer)}");
        return Task.FromResult(0);
    }
}
