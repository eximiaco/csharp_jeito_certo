using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Kafka;
using Eximia.Integrations.IntegrationEvents;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Routing;

namespace Eximia.Integrations.DomainServices;

public class JsonSchemaService
{
    private readonly TopicNameFactoryPeerEnviroment _topicNameFactory;

    public JsonSchemaService(TopicNameFactoryPeerEnviroment topicNameFactory)
    {
        _topicNameFactory = topicNameFactory;
    }

    /// <summary>
    ///     Cria o evento de integração de UMA rota a partir do comando recebido. O destino, a fila, o modo e a
    ///     exigência de callback vêm da rota; o resto vem do próprio comando.
    /// </summary>
    public ResultAggregate<IntegrationEvent> CreateFrom(
        RegisterMultipleRoutesIntegrationEventCommand command,
        Route route,
        string payloadFile,
        Guid fileId)
    {
        var topic = string.IsNullOrEmpty(route.TopicName)
            ? _topicNameFactory.CreateForDestiny(route.Destination, command.Entity)
            : _topicNameFactory.Create(route.TopicName);

        return IntegrationEvent.CreateNewIntegrationEvent(
            command.Entity,
            command.Origin,
            route.Destination,
            command.DestinyKey,
            command.Method,
            command.CorrelationId,
            command.OriginKey,
            topic,
            command.AggregateKey,
            command.SapQueryAction,
            command.StatusCode,
            command.Version,
            command.IsCallback,
            route.CallbackRequired,
            route.Mode,
            command.IsCallbackError,
            isPayloadLink: true,
            payloadFile: payloadFile,
            fileId: fileId);
    }
}
