using OrderService.Common;
using OrderService.Data;

namespace OrderService.Consumers;

public class ShippingScheduledConsumer(
    IServiceScopeFactory scopeFactory,
    ILogger<ShippingScheduledConsumer> logger) : IHostedService
{
    private readonly QueueService _queueService = scopeFactory
        .CreateScope()
        .ServiceProvider
        .GetRequiredService<QueueService>();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _queueService.Subscribe<ShippingScheduledEvent>(QueueNames.ShippingScheduledQueue, async (shippingScheduledEvent) =>
        {
            using var scope = scopeFactory.CreateScope();
            
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
            var order = dbContext.Orders.First(or => or.Id == shippingScheduledEvent.OrderId);
            order.SetAsFinished();
            await dbContext.SaveChangesAsync(cancellationToken);
    
            logger.LogInformation("Order {OrderId} processing finished", order.Id);
        });
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _queueService.Dispose();
        logger.LogInformation($"Stopping {nameof(ShippingScheduledConsumer)}");
        return Task.FromResult(0);
    }
}
