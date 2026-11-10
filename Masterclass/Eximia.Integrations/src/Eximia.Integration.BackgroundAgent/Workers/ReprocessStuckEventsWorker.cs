using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Publishing;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Outbox.EntityFramework;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Shared;
using ILogger = Serilog.ILogger;

namespace Eximia.Integration.BackgroundAgent.Workers;

/// <summary>
///     Devolve para a fila os eventos que ficaram parados em Processing/InQueue por mais de 30 minutos.
/// </summary>
public class ReprocessStuckEventsWorker(
    EximiaIntegrationDbContext dbContext,
    RoutingDataAccess routingDataAccess,
    IPublisher publisher,
    ILogger logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        dbContext.Database.ExecuteSqlRaw(
            "CREATE INDEX IF NOT EXISTS ix_integration_events_status_start_time ON integration_events (status, start_time)");

        while (true)
        {
            try
            {
                var limit = DateTime.UtcNow.AddMinutes(-30);

                var stuckEvents = await dbContext.IntegrationEvents
                    .Where(e => (e.Control.IntegrationStatus == EIntegrationStatus.Processing
                                 || e.Control.IntegrationStatus == EIntegrationStatus.InQueue)
                                && (e.Control.StartedAt ?? e.Control.CreatedAt) < limit)
                    .OrderBy(e => e.Id)
                    .Take(100)
                    .ToListAsync();

                foreach (var integrationEvent in stuckEvents)
                {
                    var route = await routingDataAccess.GetRoute(
                        integrationEvent.Entity.Entity,
                        integrationEvent.Entity.Destiny.Name!);
                    if (route.HasNoValue)
                        continue;

                    integrationEvent.RegisterRetry("Reprocessed by background agent");

                    await publisher.PublishAsync(
                        RouteIntegrationEventRequestCommand.Create(
                            integrationEvent,
                            integrationEvent.Entity.Origin.Name!,
                            integrationEvent.Entity.Destiny.Name!,
                            integrationEvent.Entity.Entity,
                            route.Value.Id));
                }

                await dbContext.SaveChangesAsync();
                logger.Information("{Count} integration events reprocessed.", stuckEvents.Count);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Error reprocessing stuck integration events.");
            }

            await Task.Delay(TimeSpan.FromMinutes(5));
        }
    }
}
