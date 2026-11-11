using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Publishing;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Outbox.EntityFramework;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Postgres;
using ILogger = Serilog.ILogger;

namespace Eximia.Integration.BackgroundAgent.Workers;

/// <summary>
///     Devolve para a fila os eventos que ficaram parados em Processing por mais de 30 minutos.
/// </summary>
public class ReprocessStuckEventsWorker(
    // PROBLEMA: DbContext no construtor de um singleton. Vive o tempo do processo: leak do change tracker,
    // dados velhos, sem thread-safety.
    EximiaIntegrationDbContext dbContext,
    RoutingDataAccess routingDataAccess,
    IPublisher publisher,
    PostgresAdvisoryLock advisoryLock,
    ILogger logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);
    private const string LockName = "eximia-reprocess-stuck-events";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Várias réplicas do agent podem estar de pé: só UMA trabalha por vez.
                await using var lease = await advisoryLock.TryAcquireAsync(LockName, stoppingToken);
                if (lease is null)
                    continue;

                var limit = DateTime.UtcNow.AddMinutes(-30);

                var stuckEvents = await dbContext.IntegrationEvents
                    .Where(e => e.Control.IntegrationStatus == EIntegrationStatus.Processing
                                && (e.Control.StartedAt ?? e.Control.CreatedAt) < limit)
                    .OrderBy(e => e.Id)
                    .Take(100)
                    .ToListAsync(stoppingToken);

                foreach (var integrationEvent in stuckEvents)
                {
                    var route = await routingDataAccess.GetRoute(
                        integrationEvent.Entity.Entity,
                        integrationEvent.Entity.Destiny.Name!,
                        stoppingToken);
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

                await dbContext.SaveChangesAsync(stoppingToken);
                logger.Information("{Count} integration events reprocessed.", stuckEvents.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.Error(ex, "Error reprocessing stuck integration events.");
            }
        }
    }
}
