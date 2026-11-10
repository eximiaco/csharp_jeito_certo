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
///     <see cref="EIntegrationStatus.InQueue" />.
/// </summary>
public sealed class EximiaOutboxReader(
    IOutboxReader inner,
    PostgresConnectionStringFactory connectionStringFactory,
    ILogger logger) : IOutboxReader
{
    private const string InQueueSql = @"UPDATE integration_events SET status = @InQueue
                                        WHERE unique_id = ANY(@UniqueIds) AND status = @InOutbox";

    public Task<IDisposableAsyncEnumerable<OutboxMessage>> GetAsync(int count) => inner.GetAsync(count);

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

            logger.Debug("Outbox produce message with success. {IntegrationId}", id);

            if (Guid.TryParse(id, out var uniqueId))
                uniqueIds.Add(uniqueId);
        }

        if (uniqueIds.Count > 0)
            MarkAsInQueueAsync(uniqueIds).GetAwaiter().GetResult();
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
