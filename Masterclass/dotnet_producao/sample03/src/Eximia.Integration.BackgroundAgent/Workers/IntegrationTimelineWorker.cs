using Eximia.Integrations.PlatformEvents;
using ILogger = Serilog.ILogger;

namespace Eximia.Integration.BackgroundAgent.Workers;

/// <summary>Grava a linha do tempo de cada integração.</summary>
public sealed class IntegrationTimelineWorker(
    PlatformEventsBus bus,
    IServiceScopeFactory scopes,
    ILogger logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        bus.Subscribe(RecordAsync);
        bus.StartReceive();
        return Task.CompletedTask;
    }

    private Task RecordAsync(PlatformEventContext context)
    {
        for (var attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                using var scope = scopes.CreateScope();
                scope.ServiceProvider.GetRequiredService<PlatformEventTimelineDataAccess>()
                    .RecordAsync(context, CancellationToken.None)
                    .GetAwaiter().GetResult();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Failed to record timeline for {ControlCode} (attempt {Attempt})",
                    context.Event.ControlCode, attempt);
                Thread.Sleep(TimeSpan.FromSeconds(2 * attempt));
            }
        }

        return Task.CompletedTask;
    }
}
