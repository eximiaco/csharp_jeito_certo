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
///     O worker é um singleton: ele só agenda. O trabalho, que usa o DbContext, roda num ESCOPO criado a cada
///     iteração, para que o DbContext nasça e morra com a iteração.
/// </summary>
public class ReprocessStuckEventsWorker(IServiceScopeFactory scopes, ILogger logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ReprocessStuckEvents>().RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.Error(ex, "Error reprocessing stuck integration events.");
            }
        }
    }
}

public class ReprocessStuckEvents(
    EximiaIntegrationDbContext dbContext,
    RoutingDataAccess routingDataAccess,
    IPublisher publisher,
    PostgresAdvisoryLock advisoryLock,
    ILogger logger)
{
    private const string LockName = "eximia-reprocess-stuck-events";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // Várias réplicas do agent podem estar de pé: só UMA trabalha por vez.
        await using var lease = await advisoryLock.TryAcquireAsync(LockName, cancellationToken);
        if (lease is null)
            return;

        var limit = DateTime.UtcNow.AddMinutes(-30);

        var stuckEvents = await dbContext.IntegrationEvents
            .Where(e => e.Control.IntegrationStatus == EIntegrationStatus.Processing
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
}
