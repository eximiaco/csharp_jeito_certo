using OrderService.Common;

namespace PaymentService
{
    public class ShippingCanceledConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<ShippingCanceledConsumer> logger) : IHostedService
    {
        private readonly QueueService _queueService = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QueueService>();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _queueService.Subscribe<UndoShippingSchedulingEvent>(QueueNames.UndoShippingSchedulingQueue, async (order) =>
            {
                // Payment canceling logic
                logger.LogError("Cancelling payment for order {OrderId}", order.OrderId);
                await _queueService.PublishAsync(QueueNames.UndoPaymentProcessingQueue, new UndoPaymentProcessingEvent(order.OrderId));
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _queueService.Dispose();
            logger.LogInformation($"Stopping {nameof(ShippingCanceledConsumer)}");
            return Task.FromResult(0);
        }
    }
}
