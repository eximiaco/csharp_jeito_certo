using Confluent.Kafka;
using Silverback.Messaging.Configuration;
using Eximia.Integrations.Events;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Shared.Kafka;

namespace Eximia.Integrations.EventDispatcher.Infrastructure;

public class KafkaClientsConfigurator : IBrokerClientsConfigurator
{
    // Valores padrão que o Silverback 4 aplicava e que o 5 alterou (BackpressureLimit 50 / MaxDegreeOfParallelism 100).
    private const int DefaultBackpressureLimit = 1;
    private const int DefaultMaxDegreeOfParallelism = 10;

    private readonly KafkaConfig _configuration;

    public KafkaClientsConfigurator(
        KafkaConfig configuration)
    {
        _configuration = configuration;
    }

    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder.AddKafkaClients(clients =>
        {
            clients.WithEximiaConnection(_configuration);

            var inboundIndex = 0;
            foreach (var inbound in _configuration.Consumer.Inbounds)
            {
                var topics = inbound.Topics!
                    .Split(",")
                    .Select(c => c)
                    .ToArray();

                var name = $"route-integration-event-{inboundIndex++}";

                clients.AddConsumer(name, consumer => consumer
                    .WithGroupId(_configuration.Connection.GroupId!)
                    .WithAutoOffsetReset((AutoOffsetReset)inbound.AutoOffsetReset!)
                    .LimitBackpressure(inbound.BackpressureLimit is > 0 ? inbound.BackpressureLimit.Value : DefaultBackpressureLimit)
                    .LimitParallelism(inbound.MaxDegreeOfParallelism is > 0 ? inbound.MaxDegreeOfParallelism.Value : DefaultMaxDegreeOfParallelism)
                    .Consume<RouteIntegrationEventRequestCommand>(name, endpoint => endpoint
                        .ConsumeFrom(topics)
                        .OnError(policy => policy.Retry(retry => retry
                            .WithIncrementalDelay(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(50))))
                        .DisableMessageValidation()
                        .DeserializeJson(serializer => serializer.UseModel<RouteIntegrationEventRequestCommand>())));
            }

            clients.AddConsumer("process-integration-event-callback", consumer => consumer
                .WithGroupId(_configuration.Connection.GroupId!)
                .WithAutoOffsetReset((AutoOffsetReset)_configuration.Callback.AutoOffsetReset!)
                .LimitBackpressure(DefaultBackpressureLimit)
                .LimitParallelism(DefaultMaxDegreeOfParallelism)
                .Consume<ProcessIntegrationEventCallbackCommand>("process-integration-event-callback", endpoint => endpoint
                    .ConsumeFrom(_configuration.Callback.Topic)));

            clients.AddProducer("callback-producer", producer => producer
                .Produce<ProcessIntegrationEventCallbackCommand>("callback", endpoint => endpoint
                    .ProduceTo((ProcessIntegrationEventCallbackCommand _) => _configuration.Callback.Topic)
                    .SetKafkaKey(message => message.Key)
                    .SerializeAsJson()
                    .DisableMessageValidation()));

            // Evento de observabilidade do painel: não espera o ack do Kafka para liberar o handler.
            clients.AddProducer("plataform-event-producer", producer => producer
                .DisableDeliveryReports()
                .IgnoreIfNotAcknowledged()
                .Produce<PlataformEvent>("plataform-event", endpoint => endpoint
                    .ProduceTo((PlataformEvent _) => _configuration.Events.Topic)
                    .SetKafkaKey(message => message.ControlCode)
                    .SerializeAsJson()
                    .DisableMessageValidation()));
        });
    }
}
