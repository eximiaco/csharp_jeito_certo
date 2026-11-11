using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Publishing;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Outbox.EntityFramework;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Shared;
using ILogger = Serilog.ILogger;

namespace Eximia.Integration.BackgroundAgent.Workers;

/// <summary>
///     Devolve para a fila os eventos que ficaram parados em Processing por mais de 30 minutos.
///     O worker é um singleton: ele só agenda. O trabalho, que usa o DbContext, roda num ESCOPO criado a cada
///     iteração, para que o DbContext nasça e morra com a iteração.
/// </summary>
public class ReprocessStuckEventsWorker(IServiceScopeFactory scopes, ILogger logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // PROBLEMA: while(true) que ignora o token. O StopAsync espera até 5 minutos: o deploy estoura o ShutdownTimeout.
        while (true)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<ReprocessStuckEvents>().RunAsync();
            }
            // PROBLEMA: engole também OperationCanceledException: mesmo com token, o serviço seguiria vivo.
            catch (Exception ex)
            {
                logger.Error(ex, "Error reprocessing stuck integration events.");
            }

            // PROBLEMA: espera sem token, e sem exclusão entre réplicas (duas instâncias reprocessam os mesmos eventos).
            await Task.Delay(TimeSpan.FromMinutes(5));
        }
    }
}

/// <summary>Uma rodada de reprocessamento. É scoped: recebe o DbContext da iteração.</summary>
public class ReprocessStuckEvents(
    EximiaIntegrationDbContext dbContext,
    RoutingDataAccess routingDataAccess,
    IPublisher publisher,
    ILogger logger)
{
    public async Task RunAsync()
    {
        var limit = DateTime.UtcNow.AddMinutes(-30);

        var stuckEvents = await dbContext.IntegrationEvents
            .Where(e => e.Control.IntegrationStatus == EIntegrationStatus.Processing
                        && (e.Control.StartedAt ?? e.Control.CreatedAt) < limit)
            .OrderBy(e => e.Id)
            .Take(100)
            // PROBLEMA: query sem token. O EF não pode ser interrompido no shutdown.
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

        // PROBLEMA: gravação sem token. O EF não pode ser interrompido no shutdown.
        await dbContext.SaveChangesAsync();
        logger.Information("{Count} integration events reprocessed.", stuckEvents.Count);
    }
}
