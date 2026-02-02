using System.Text.Json;
using GymErp.Common;
using GymErp.Domain.Subscriptions.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GymErp.Domain.Subscriptions.Features.SuspendEnrollment;

public sealed class OutboxPublisherHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisherHostedService> logger)
    : BackgroundService
{
    private const int PollingIntervalSeconds = 5;
    private const int BatchSize = 50;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outbox publisher started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var tenantLocator = scope.ServiceProvider.GetRequiredService<ITenantLocator>();
                var dbContextFactory = scope.ServiceProvider.GetRequiredService<IEfDbContextFactory<SubscriptionsDbContext>>();
                var serviceBus = scope.ServiceProvider.GetRequiredService<IServiceBus>();

                var tenants = await tenantLocator.GetAll(stoppingToken);

                foreach (var tenant in tenants)
                {
                    try
                    {
                        await using var context = await dbContextFactory.CriarAsync(tenant.Name);

                        var messages = await context.OutboxMessages
                            .OrderBy(m => m.CreatedAt)
                            .Take(BatchSize)
                            .ToListAsync(stoppingToken);

                        foreach (var message in messages)
                        {
                            try
                            {
                                var type = Type.GetType(message.Type);
                                if (type == null)
                                {
                                    logger.LogWarning("Outbox message type not found: {Type}", message.Type);
                                    continue;
                                }

                                var payload = JsonSerializer.Deserialize(message.Payload, type, JsonOptions);
                                if (payload == null)
                                {
                                    logger.LogWarning("Failed to deserialize outbox message {Id}", message.Id);
                                    continue;
                                }

                                await serviceBus.PublishAsync(payload);

                                context.OutboxMessages.Remove(message);
                                await context.SaveChangesAsync(stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Error publishing outbox message {Id}, will retry next cycle", message.Id);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing outbox for tenant {TenantName}", tenant.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in outbox publisher cycle");
            }

            await Task.Delay(TimeSpan.FromSeconds(PollingIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("Outbox publisher stopped");
    }
}
