using OrderService;
using OrderService.Common;

namespace StockService
{
    public class OrderCreatedConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderCreatedConsumer> logger) : IHostedService
    {
        private readonly QueueService _queueService = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QueueService>();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _queueService.Subscribe<OrderCreatedEvent>(QueueNames.OrderCreatedQueue, async (order) =>
            {
                logger.LogInformation("Stock reserverd for order {OrderId}", order.OrderId);
                // Reserve stock logic
                await _queueService.PublishAsync(QueueNames.StockReservedQueue, new StockReservedEvent(order.OrderId));
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _queueService.Dispose();
            logger.LogInformation($"Stopping {nameof(OrderCreatedConsumer)}");
            return Task.FromResult(0);
        }
    }
}
