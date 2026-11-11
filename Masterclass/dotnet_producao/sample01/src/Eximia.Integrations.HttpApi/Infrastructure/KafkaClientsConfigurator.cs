using Silverback.Messaging.Configuration;
using Eximia.Integrations.Events;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Shared.Kafka;
using Eximia.Integrations.Shared.Postgres;

namespace Eximia.Integrations.HttpApi.Infrastructure;

public class KafkaClientsConfigurator : IBrokerClientsConfigurator
{
    public const string ProducerName = "integration-producer";
    public const string RouteIntegrationEventEndpointName = "route-integration-event-request";
    public const string PlataformEventEndpointName = "plataform-event";

    private readonly KafkaConfig _configuration;
    private readonly string _outboxConnectionString;

    public KafkaClientsConfigurator(
        KafkaConfig configuration,
        PostgresConnectionStringFactory connectionStringFactory)
    {
        _configuration = configuration;
        _outboxConnectionString = connectionStringFactory.CreateConnectionString().ConnectionString;
    }

    public void Configure(BrokerClientsConfigurationBuilder builder)
    {
        builder.AddKafkaClients(clients => clients
            .WithEximiaConnection(_configuration)
            .AddProducer(ProducerName, producer => producer
                .Produce<RouteIntegrationEventRequestCommand>(RouteIntegrationEventEndpointName, endpoint => endpoint
                    .ProduceTo(message => message.Topic)
                    .SetKafkaKey(message => message.Key)
                    .SerializeAsJson()
                    .DisableMessageValidation()
                    .StoreToOutbox(outbox => outbox.UsePostgreSql(_outboxConnectionString)))
                .Produce<PlataformEvent>(PlataformEventEndpointName, endpoint => endpoint
                    .ProduceTo((PlataformEvent _) => _configuration.Events.Topic)
                    .SetKafkaKey(message => message.ControlCode)
                    .SerializeAsJson()
                    .DisableMessageValidation()
                    .StoreToOutbox(outbox => outbox.UsePostgreSql(_outboxConnectionString)))));
    }
}
