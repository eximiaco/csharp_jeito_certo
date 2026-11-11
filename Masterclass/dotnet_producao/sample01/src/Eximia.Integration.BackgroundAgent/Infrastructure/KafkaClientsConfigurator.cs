using Silverback.Messaging.Configuration;
using Eximia.Integrations.Events;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Shared.Kafka;

namespace Eximia.Integration.BackgroundAgent.Infrastructure;

public class KafkaClientsConfigurator : IBrokerClientsConfigurator
{
    private readonly KafkaConfig _configuration;
    private readonly TopicNameFactoryPeerEnviroment _topicNameFactoryPeerEnviroment;

    public KafkaClientsConfigurator(KafkaConfig configuration, TopicNameFactoryPeerEnviroment topicNameFactoryPeerEnviroment)
    {
        _configuration = configuration;
        _topicNameFactoryPeerEnviroment = topicNameFactoryPeerEnviroment;
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
                    .ProduceTo((PlataformEvent _) => _topicNameFactoryPeerEnviroment.Create(_configuration.Events.Topic))
                    .SetKafkaKey(message => message.ControlCode)
                    .SerializeAsJson()
                    .DisableMessageValidation())));
    }
}
