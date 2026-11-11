using Silverback.Messaging.Configuration;
using Eximia.Integrations.Events;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Shared.Kafka;

namespace Eximia.Integration.BackgroundAgent.Infrastructure;

public class KafkaClientsConfigurator : IBrokerClientsConfigurator
{
    private readonly KafkaConfig _configuration;

    public KafkaClientsConfigurator(KafkaConfig configuration)
    {
        _configuration = configuration;
    }

    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder.AddKafkaClients(clients => clients
            .WithEximiaConnection(_configuration)
            .AddProducer("integration-producer", producer => producer
                .Produce<RouteIntegrationEventRequestCommand>("route-integration-event-request", endpoint => endpoint
                    .ProduceTo(message => message.Topic)
                    .SetKafkaKey(message => message.Key)
                    .SerializeAsJson()
                    .DisableMessageValidation()))
            .AddProducer("plataform-event-producer", producer => producer
                .Produce<PlataformEvent>("plataform-event", endpoint => endpoint
                    .ProduceTo((PlataformEvent _) => _configuration.Events.Topic)
                    .SetKafkaKey(message => message.ControlCode)
                    .SerializeAsJson()
                    .DisableMessageValidation())));
    }
}
