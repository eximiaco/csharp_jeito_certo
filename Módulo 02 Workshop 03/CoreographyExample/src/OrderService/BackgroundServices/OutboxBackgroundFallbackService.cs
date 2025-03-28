using Microsoft.EntityFrameworkCore;
using OrderService.Common;
using OrderService.Data;

namespace OrderService.BackgroundServices;

public class OutboxBackgroundFallbackService(
    IServiceScopeFactory scopeFactory, 
    ILogger<OutboxBackgroundFallbackService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
                var queueService = scope.ServiceProvider.GetRequiredService<QueueService>();
                var messagesToPublish = await dbContext
                    .OutboxMessages
                    .ToListAsync(cancellationToken: stoppingToken);

                foreach (var message in messagesToPublish)
                {
                    await queueService.PublishAsync(message.QueueName, message.Payload);
                    dbContext.Remove(message);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error ocurred processing outbox messages");
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
} 