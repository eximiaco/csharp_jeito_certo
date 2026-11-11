using Eximia.Integrations.Events;
using Eximia.Integrations.PlatformEvents;
using ILogger = Serilog.ILogger;

namespace Eximia.Integration.BackgroundAgent.Workers;

/// <summary>Registra um alerta quando uma integração falha.</summary>
public sealed class IntegrationErrorAlertWorker(PlatformEventsBus bus, ILogger logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bus.Subscribe(AlertAsync);
        // PROBLEMA: parte síncrona do ExecuteAsync mexendo em estado compartilhado.
        // No .NET 10 roda em paralelo com o outro serviço.
        bus.StartReceive();
        return Task.CompletedTask;
    }

    private Task AlertAsync(PlatformEventContext context)
    {
        if (context.Event.Name == PlataformEventType.IntegrationEventError)
            logger.Warning("Integration {ControlCode} failed.", context.Event.ControlCode);

        return Task.CompletedTask;
    }
}
