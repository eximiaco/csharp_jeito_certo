using System.Text.Json.Nodes;
using CSharpFunctionalExtensions;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.IntegrationEvents.Commands;

public record RegisterMultipleRoutesIntegrationEventCommand
{
    public string Origin { get; }
    public string OriginKey { get; }
    public string Destiny { get; }
    public string DestinyKey { get; }
    public string Entity { get; }
    public string Method { get; }
    public string CorrelationId { get; }
    public JsonNode Payload { get; }
    public string AggregateKey { get; }
    public int SchemaVersion { get; }
    public bool IsCallback { get; }
    public bool IsCallbackError { get; }
    public string? SapQueryAction { get; }
    public int? StatusCode { get; }
    public long Version { get; }
    public int? IntegrationEventId { get; }

    private RegisterMultipleRoutesIntegrationEventCommand(
        string origin,
        string originKey,
        string destiny,
        string destinyKey,
        string entity,
        string method,
        string correlationId,
        JsonNode payload,
        string aggregateKey,
        int schemaVersion,
        bool isCallback,
        bool isCallbackError,
        string? sapQueryAction,
        int? statusCode,
        long version,
        int? integrationEventId)
    {
        Origin = origin;
        OriginKey = originKey;
        Destiny = destiny;
        DestinyKey = destinyKey;
        Entity = entity;
        Method = method;
        CorrelationId = correlationId;
        Payload = payload;
        AggregateKey = aggregateKey;
        SchemaVersion = schemaVersion;
        IsCallback = isCallback;
        IsCallbackError = isCallbackError;
        SapQueryAction = sapQueryAction;
        StatusCode = statusCode;
        Version = version;
        IntegrationEventId = integrationEventId;
    }

    public static ResultAggregate<RegisterMultipleRoutesIntegrationEventCommand> Create(
        string origin,
        string originKey,
        string destiny,
        string destinyKey,
        string entity,
        string method,
        string correlationId,
        JsonNode payload,
        string aggregateKey,
        int schemaVersion,
        bool isCallback,
        bool isCallbackError,
        string? sapQueryAction,
        int? statusCode,
        long version,
        int? integrationEventId)
    {
        var result = ResultAggregate.Combine(
            ResultAggregate.FailureIf(string.IsNullOrEmpty(destiny), "[Destiny]", "Destiny is invalid"),
            ResultAggregate.FailureIf(string.IsNullOrEmpty(origin), "[Origin]", "Origin is invalid"),
            ResultAggregate.FailureIf(!isCallback && string.IsNullOrEmpty(originKey), "[OriginKey]", "Origin Key is invalid"),
            ResultAggregate.FailureIf(string.IsNullOrEmpty(entity), "[Entity]", "Entity is invalid"),
            ResultAggregate.FailureIf(string.IsNullOrEmpty(method), "[Method]", "Method is invalid"),
            ResultAggregate.FailureIf(string.IsNullOrEmpty(correlationId), "[CorrelationId]", "CorrelationId is invalid"),
            ResultAggregate.FailureIf(payload == null, "[Payload]", "Payload is invalid"));

        if (result.IsFailure)
            return result.ConvertFailure<RegisterMultipleRoutesIntegrationEventCommand>();

        return new RegisterMultipleRoutesIntegrationEventCommand(
            origin,
            originKey,
            destiny,
            destinyKey,
            entity,
            method,
            correlationId,
            payload,
            aggregateKey,
            schemaVersion,
            isCallback,
            isCallbackError,
            sapQueryAction,
            statusCode,
            version,
            integrationEventId);
    }
}
