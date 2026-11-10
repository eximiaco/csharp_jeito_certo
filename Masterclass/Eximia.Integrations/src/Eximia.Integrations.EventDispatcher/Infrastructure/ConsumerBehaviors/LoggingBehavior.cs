using Serilog.Context;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.EventDispatcher.Infrastructure.ConsumerBehaviors;

public class LoggingBehavior(OtelVariables otelVariables, SquadsSingleton squadsSingleton) : IConsumerBehavior
{
    public int SortIndex => 0;

    public async ValueTask HandleAsync(
        ConsumerPipelineContext context,
        ConsumerBehaviorHandler next,
        CancellationToken cancellationToken)
    {
        var kafkaOffset = context.Envelope.GetKafkaOffset();
        if (kafkaOffset is null)
        {
            await next(context, cancellationToken);
            return;
        }

        var entity = context.Envelope.Headers.GetValue("x-integration-entity") ?? string.Empty;
        using (LogContext.PushProperty(otelVariables.KafkaTopicOffset, $"[{kafkaOffset.TopicPartition.Partition.Value}]@{kafkaOffset.Offset.Value}"))
        using (LogContext.PushProperty(otelVariables.KafkaTopicName, kafkaOffset.TopicPartition.Topic))
        using (LogContext.PushProperty(otelVariables.KafkaMessageKey, context.Envelope.Headers.GetValue("x-kafka-message-key") ?? string.Empty))
        using (LogContext.PushProperty(otelVariables.KafkaMessageId, context.Envelope.Headers.GetValue("x-message-id") ?? string.Empty))
        using (LogContext.PushProperty(otelVariables.ControlCode, context.Envelope.Headers.GetValue("x-controlCode") ?? string.Empty))
        using (LogContext.PushProperty(otelVariables.IntegrationUniqueId, context.Envelope.Headers.GetValue("x-integration-id") ?? string.Empty))
        using (LogContext.PushProperty(otelVariables.Origin, context.Envelope.Headers.GetValue("x-integration-origin") ?? string.Empty))
        using (LogContext.PushProperty(otelVariables.Destiny, context.Envelope.Headers.GetValue("x-integration-destiny") ?? string.Empty))
        using (LogContext.PushProperty(otelVariables.Entity, entity))
        using (LogContext.PushProperty(otelVariables.Squad, squadsSingleton.GetByEntity(entity)))
            await next(context, cancellationToken);
    }
}
