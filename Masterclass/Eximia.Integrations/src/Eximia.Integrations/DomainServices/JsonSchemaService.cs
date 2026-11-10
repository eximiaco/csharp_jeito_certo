using System.Text.Json;
using System.Text.Json.Nodes;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Kafka;
using Eximia.Integrations.IntegrationEvents;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Routing;

namespace Eximia.Integrations.DomainServices;

public class JsonSchemaService : IService<JsonSchemaService>
{
    private readonly TopicNameFactoryPeerEnviroment _topicNameFactory;

    public JsonSchemaService(TopicNameFactoryPeerEnviroment topicNameFactory)
    {
        _topicNameFactory = topicNameFactory;
    }

    public ResultAggregate<IntegrationEvent> CreateFrom(
        RegisterAsyncIntegrationEventCommand integrationRequest,
        Route route,
        bool isPayloadLink = false,
        string payloadFile = "",
        Guid fileId = default)
    {
        return CreateFrom(
            integrationRequest.Destiny,
            integrationRequest.DestinyKey,
            integrationRequest.Origin,
            integrationRequest.OriginKey,
            integrationRequest.Entity,
            integrationRequest.Method,
            integrationRequest.CorrelationId,
            integrationRequest.IsCallback,
            integrationRequest.SapQueryAction,
            integrationRequest.StatusCode,
            integrationRequest.Version,
            route.CallbackRequired,
            route.TopicName,
            integrationRequest.AggregateKey!,
            route.Mode,
            integrationRequest.IsCallbackError,
            isPayloadLink,
            payloadFile,
            fileId);
    }

    public ResultAggregate<IntegrationEvent> CreateFrom(
        string destiny,
        string destinyKey,
        string origin,
        string originKey,
        string entity,
        string method,
        string correlationId,
        bool isCallback,
        string? sapQueryAction,
        int? statusCode,
        long version,
        bool requireCallback = false,
        string destinationQueue = "",
        string aggregateKey = "",
        EMode mode = EMode.PROD,
        bool isCallbackError = false,
        bool isPayloadLink = false,
        string payloadFile = "",
        Guid fileId = default)
    {
        var topic = string.IsNullOrEmpty(destinationQueue)
            ? _topicNameFactory.CreateForDestiny(destiny, entity)
            : _topicNameFactory.Create(destinationQueue);

        return IntegrationEvent.CreateNewIntegrationEvent(
            entity,
            origin,
            destiny,
            destinyKey,
            method,
            correlationId,
            originKey,
            topic,
            aggregateKey,
            sapQueryAction,
            statusCode,
            version,
            isCallback,
            requireCallback,
            mode,
            isCallbackError,
            isPayloadLink: isPayloadLink,
            payloadFile: payloadFile,
            fileId: fileId);
    }
}
