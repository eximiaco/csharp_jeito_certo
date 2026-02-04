using System.Collections.Concurrent;
using System.Text.Json;
using GymErp.Common.RabbitMq;
using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace GymErp.Common.Infrastructure;

public class RabbitMqServiceBus(IConnection connection, IOptions<RabbitMqConfig> options) : IServiceBus
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly RabbitMqConnectionConfig _config = options.Value.Connection;
    private static readonly ConcurrentDictionary<Type, string> ExchangeByType = new();

    static RabbitMqServiceBus()
    {
        ExchangeByType[typeof(EnrollmentCreatedEvent)] = RabbitMqTopology.ExchangeEnrollmentEvents;
        ExchangeByType[typeof(ChargingProcessedEvent)] = RabbitMqTopology.ExchangeChargingProcessedEvents;
        ExchangeByType[typeof(CancelEnrollmentCommand)] = RabbitMqTopology.ExchangeCancelEnrollmentCommands;
    }

    public async Task PublishAsync(object message)
    {
        var type = message.GetType();
        if (!ExchangeByType.TryGetValue(type, out var exchange))
            throw new ArgumentException($"Unsupported message type: {type.Name}", nameof(message));

        var body = JsonSerializer.SerializeToUtf8Bytes(message, type, JsonOptions);
        IChannel? channel = null;
        try
        {
            channel = await connection.CreateChannelAsync(null, CancellationToken.None).ConfigureAwait(false);
            await channel.BasicPublishAsync(exchange, routingKey: "", mandatory: false, new ReadOnlyMemory<byte>(body), CancellationToken.None)
                .ConfigureAwait(false);
        }
        finally
        {
            channel?.Dispose();
        }
    }
}
