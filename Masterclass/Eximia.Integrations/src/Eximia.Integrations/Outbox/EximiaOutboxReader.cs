using Dapper;
using Npgsql;
using Serilog;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Postgres;

namespace Eximia.Integrations.Outbox;

/// <summary>
///     Decorator do reader padrão do Silverback (PostgreSql). Depois que o <see cref="OutboxWorker" /> confirma a
///     produção das mensagens no Kafka (<see cref="AcknowledgeAsync" />), marca os integration events como
///     <see cref="EIntegrationStatus.InQueue" /> e atualiza as métricas de publicação.
/// </summary>
public sealed class EximiaOutboxReader(
    IOutboxReader inner,
    PostgresConnectionStringFactory connectionStringFactory,
    OutboxMetricsReader outboxMetricsReader,
    OutboxMetrics outboxMetrics,
    ILogger logger) : IOutboxReader
{
    private const string InQueueSql = @"UPDATE integration_events SET status = @InQueue
                                        WHERE unique_id = ANY(@UniqueIds) AND status = @InOutbox";

    public async Task<IDisposableAsyncEnumerable<OutboxMessage>> GetAsync(int count)
    {
        // IOutboxReader.GetAsync não recebe token: não há de onde propagar.
        var queue = await outboxMetricsReader.GetQueue(CancellationToken.None).ConfigureAwait(false);
        outboxMetrics.UpdateTotalQueue(queue);

        return await inner.GetAsync(count).ConfigureAwait(false);
    }

    public async Task AcknowledgeAsync(IEnumerable<OutboxMessage> outboxMessages)
    {
        var messages = outboxMessages.ToList();

        await inner.AcknowledgeAsync(messages).ConfigureAwait(false);

        if (messages.Count == 0)
            return;

        var uniqueIds = new List<Guid>(messages.Count);
        foreach (var message in messages)
        {
            var id = message.Headers?.FirstOrDefault(h => h.Name == OutboxDefaults.HeaderIntegrationId)?.Value;
            var origin = message.Headers?.FirstOrDefault(h => h.Name == OutboxDefaults.HeaderOrigin)?.Value;
            var destiny = message.Headers?.FirstOrDefault(h => h.Name == OutboxDefaults.HeaderDestiny)?.Value;
            var entity = message.Headers?.FirstOrDefault(h => h.Name == OutboxDefaults.HeaderEntity)?.Value;

            logger.Debug("Outbox produce message with success. {IntegrationId}", id);
            outboxMetrics.AddPublishedMessages(origin, destiny, entity);

            if (Guid.TryParse(id, out var uniqueId))
                uniqueIds.Add(uniqueId);
        }

        if (uniqueIds.Count > 0)
            await MarkAsInQueueAsync(uniqueIds).ConfigureAwait(false);
    }

    public Task<int> GetLengthAsync() => inner.GetLengthAsync();

    public Task<TimeSpan> GetMaxAgeAsync() => inner.GetMaxAgeAsync();

    private async Task MarkAsInQueueAsync(List<Guid> uniqueIds)
    {
        await using var connection = new NpgsqlConnection(connectionStringFactory.CreateConnectionString().ConnectionString);
        await connection.ExecuteAsync(InQueueSql, new
        {
            InQueue = (int)EIntegrationStatus.InQueue,
            InOutbox = (int)EIntegrationStatus.InOutbox,
            UniqueIds = uniqueIds.ToArray()
        }).ConfigureAwait(false);
    }
}
