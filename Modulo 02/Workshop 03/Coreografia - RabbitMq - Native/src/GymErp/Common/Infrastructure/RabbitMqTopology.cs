using RabbitMQ.Client;

namespace GymErp.Common.Infrastructure;

public static class RabbitMqTopology
{
    public const string ExchangeEnrollmentEvents = "enrollment-events";
    public const string ExchangeChargingProcessedEvents = "charging-processed-events";
    public const string ExchangeCancelEnrollmentCommands = "cancel-enrollment-commands";
    public const string QueueEnrollmentEventsFinancial = "enrollment-events-financial-module";
    public const string QueueCancelEnrollmentCommandsSubscriptions = "cancel-enrollment-commands-subscriptions-module";

    public static async Task DeclareExchangesAndQueuesAsync(IChannel channel, CancellationToken cancellationToken = default)
    {
        await channel.ExchangeDeclareAsync(ExchangeEnrollmentEvents, ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null, passive: false, noWait: false, cancellationToken).ConfigureAwait(false);
        await channel.ExchangeDeclareAsync(ExchangeChargingProcessedEvents, ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null, passive: false, noWait: false, cancellationToken).ConfigureAwait(false);
        await channel.ExchangeDeclareAsync(ExchangeCancelEnrollmentCommands, ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null, passive: false, noWait: false, cancellationToken).ConfigureAwait(false);

        await channel.QueueDeclareAsync(QueueEnrollmentEventsFinancial, durable: true, exclusive: false, autoDelete: false, arguments: null, passive: false, noWait: false, cancellationToken).ConfigureAwait(false);
        await channel.QueueDeclareAsync(QueueCancelEnrollmentCommandsSubscriptions, durable: true, exclusive: false, autoDelete: false, arguments: null, passive: false, noWait: false, cancellationToken).ConfigureAwait(false);

        await channel.QueueBindAsync(QueueEnrollmentEventsFinancial, ExchangeEnrollmentEvents, routingKey: "", arguments: null, noWait: false, cancellationToken).ConfigureAwait(false);
        await channel.QueueBindAsync(QueueCancelEnrollmentCommandsSubscriptions, ExchangeCancelEnrollmentCommands, routingKey: "", arguments: null, noWait: false, cancellationToken).ConfigureAwait(false);
    }
}
