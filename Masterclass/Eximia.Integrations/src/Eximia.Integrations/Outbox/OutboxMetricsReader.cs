using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
using Silverback.Messaging.Messages;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Postgres;

namespace Eximia.Integrations.Outbox;

/// <summary>
///     Lê os headers das mensagens pendentes no outbox do Silverback para alimentar as métricas de fila.
/// </summary>
public class OutboxMetricsReader(NpgsqlDataSource dataSource)
{
    private static readonly string Sql = $"SELECT headers FROM \"{OutboxDefaults.TableName}\"";

    public async Task<IEnumerable<OutboxMeasurement>> GetQueue(CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection();
        var rows = (await connection.QueryAsync<object?>(new CommandDefinition(Sql, cancellationToken: cancellationToken))).ToList();

        var queue = rows.Select(raw =>
        {
            var headers = DeserializeHeaders(raw);
            return new OutboxMeasurement(1,
                headers.FirstOrDefault(c => c.Name == OutboxDefaults.HeaderOrigin)?.Value,
                headers.FirstOrDefault(c => c.Name == OutboxDefaults.HeaderDestiny)?.Value,
                headers.FirstOrDefault(c => c.Name == OutboxDefaults.HeaderEntity)?.Value);
        });

        return queue.GroupBy(c => new { c.Origin, c.Destiny, c.Entity })
            .Select(c => new OutboxMeasurement(
                c.Sum(r => r.Total),
                c.Key.Origin,
                c.Key.Destiny,
                c.Key.Entity))
            .ToList();
    }

    private static List<MessageHeader> DeserializeHeaders(object? raw)
    {
        var json = raw switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(json))
            return [];

        return JsonSerializer.Deserialize<List<MessageHeader>>(json) ?? [];
    }
}
