using Eximia.Integrations.Outbox;
using ILogger = Serilog.ILogger;

namespace Eximia.Integration.BackgroundAgent.Workers;

/// <summary>
///     Publica a métrica de fila do outbox a cada minuto. Antes esse cálculo (varredura completa dos headers do
///     outbox) rodava dentro do polling do OutboxWorker, a cada 500 ms, no caminho que publica as mensagens.
/// </summary>
public class QueueMetricsWorker(IServiceScopeFactory scopes, ILogger logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                // Um escopo por iteração: serviços scoped nunca são capturados pelo singleton do worker.
                await using var scope = scopes.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<PublishQueueMetrics>().RunAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.Error(ex, "Falha ao publicar as métricas de fila; nova tentativa no próximo ciclo.");
            }
        }
    }
}

public class PublishQueueMetrics(OutboxMetricsReader reader, OutboxMetrics metrics)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var queue = await reader.GetQueue(cancellationToken);
        metrics.UpdateTotalQueue(queue);
    }
}
