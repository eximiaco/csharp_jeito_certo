using OrderService.Common;

namespace StockService
{
    public class PaymentCanceledConsumer(
        IServiceScopeFactory scopeFactory,
        ILogger<PaymentCanceledConsumer> logger) : IHostedService
    {
        private readonly QueueService _queueService = scopeFactory.CreateScope().ServiceProvider.GetRequiredService<QueueService>();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _queueService.Subscribe<UndoPaymentProcessingEvent>(QueueNames.UndoPaymentProcessingQueue, async (message) =>
            {
                logger.LogError("Canceling stock reservation for order {OrderId}", message.OrderId);
                // Cancel stock reservation logic
                await _queueService.PublishAsync(QueueNames.UndoReserveStockQueue, new UndoStockReservationEvent(message.OrderId));
            });
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _queueService.Dispose();
            logger.LogInformation($"Stopping {nameof(PaymentCanceledConsumer)}");
            return Task.FromResult(0);
        }
    }
}
