using OrderService.Common;
using OrderService.Data;

namespace OrderService.Consumers;

public class StockReservationCanceledConsumer(
    IServiceScopeFactory scopeFactory,
    ILogger<StockReservationCanceledConsumer> logger) : IHostedService
{
    private readonly QueueService _queueService = scopeFactory
        .CreateScope()
        .ServiceProvider
        .GetRequiredService<QueueService>();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _queueService.Subscribe<UndoStockReservationEvent>(QueueNames.UndoReserveStockQueue, async (message) =>
        {
            using var scope = scopeFactory.CreateScope();
            
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = dbContext.Orders.First(or => or.Id == message.OrderId);
            order.SetAsCanceled();
            await dbContext.SaveChangesAsync(cancellationToken);
    
            logger.LogError("Order {OrderId} canceled", order.Id);
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _queueService.Dispose();
        logger.LogInformation($"Stopping {nameof(StockReservationCanceledConsumer)}");
        return Task.FromResult(0);
    }
}
