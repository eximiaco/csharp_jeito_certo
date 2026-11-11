using Dapper;
using Npgsql;

namespace Eximia.Integrations.PlatformEvents;

/// <summary>Grava a linha do tempo de cada integração (recebido, enviado, erro) a partir dos eventos da plataforma.</summary>
public sealed class PlatformEventTimelineDataAccess(NpgsqlDataSource dataSource)
{
    // O consumer é at-least-once: a mesma mensagem pode chegar de novo (rebalance, restart antes do commit).
    // A chave natural da mensagem (topic, partition, offset) torna a gravação idempotente.
    private const string Sql = @"
        INSERT INTO integration_event_timeline
            (kafka_topic, kafka_partition, kafka_offset, control_code, event_name, event_time, payload)
        VALUES
            (@Topic, @Partition, @Offset, @ControlCode, @Name, @EventTime, CAST(@Payload AS jsonb))
        ON CONFLICT (kafka_topic, kafka_partition, kafka_offset) DO NOTHING";

    public async Task RecordAsync(PlatformEventContext context, CancellationToken cancellationToken)
    {
        await using var connection = dataSource.CreateConnection();
        await connection.ExecuteAsync(new CommandDefinition(
            Sql,
            new
            {
                context.Topic,
                context.Partition,
                context.Offset,
                context.Event.ControlCode,
                context.Event.Name,
                EventTime = context.Event.Timestamp,
                Payload = context.Event.Data.GetRawText()
            },
            cancellationToken: cancellationToken));
    }
}
