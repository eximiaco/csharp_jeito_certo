using System.Collections.Concurrent;
using System.Text.Json;
using Confluent.Kafka;
using GymErp.Common.Kafka;
using GymErp.Domain.Financial.Features.ProcessCharging;
using GymErp.Domain.Subscriptions.Aggreates.Enrollments;
using GymErp.Domain.Subscriptions.Features.CancelEnrollment;
using Microsoft.Extensions.Options;

namespace GymErp.Common.Infrastructure;

public class KafkaServiceBus : IServiceBus
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly IProducer<Null, byte[]> _producer;
    private static readonly ConcurrentDictionary<Type, string> TopicByType = new();

    static KafkaServiceBus()
    {
        TopicByType[typeof(EnrollmentCreatedEvent)] = KafkaTopics.EnrollmentEvents;
        TopicByType[typeof(ChargingProcessedEvent)] = KafkaTopics.ChargingProcessedEvents;
        TopicByType[typeof(CancelEnrollmentCommand)] = KafkaTopics.CancelEnrollmentCommands;
    }

    public KafkaServiceBus(IProducer<Null, byte[]> producer)
    {
        _producer = producer;
    }

    public async Task PublishAsync(object message)
    {
        var type = message.GetType();
        if (!TopicByType.TryGetValue(type, out var topic))
            throw new ArgumentException($"Unsupported message type: {type.Name}", nameof(message));

        var body = JsonSerializer.SerializeToUtf8Bytes(message, type, JsonOptions);
        try
        {
            await _producer.ProduceAsync(topic, new Message<Null, byte[]> { Value = body }).ConfigureAwait(false);
        }
        catch (ProduceException<Null, byte[]> ex)
        {
            throw new InvalidOperationException($"Failed to produce message to topic '{topic}': {ex.Error.Reason}", ex);
        }
    }
}
