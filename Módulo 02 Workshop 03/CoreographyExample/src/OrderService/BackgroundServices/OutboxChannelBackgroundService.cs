using OrderService.Common;
using OrderService.Data;
using System.Threading.Channels;

namespace OrderService.BackgroundServices;

public class OutboxMessageChannelConsumer(
    IServiceScopeFactory scopeFactory, 
    Channel<OutboxMessage> channel,
    ILogger<OutboxMessageChannelConsumer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var outboxMessage in channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                var queueService = scope.ServiceProvider.GetRequiredService<QueueService>();

                await queueService.PublishAsync(outboxMessage.QueueName, outboxMessage.Payload);
                dbContext.Remove(outboxMessage);
                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error ocurred processing outbox channel message");
            }
        }
    }
}