using System.Text.Json.Nodes;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.IntegrationEvents.Commands;

public record RegisterAsyncIntegrationEventCommand(
    string Destiny,
    string DestinyKey,
    string Origin,
    string OriginKey,
    string Entity,
    string Method,
    string CorrelationId,
    JsonNode Payload,
    int SchemaVersion,
    bool IsCallback,
    string? AggregateKey,
    string? SapQueryAction,
    int? StatusCode,
    long Version,
    bool IsCallbackError = false)
{
    public static ResultAggregate<RegisterAsyncIntegrationEventCommand> Create(
        string destiny,
        string destinyKey,
        string origin,
        string originKey,
        string entity,
        string method,
        string correlationId,
        JsonNode payload,
        string? aggregateKey,
        string? sapQueryAction,
        int? statusCode,
        long version,
        int schemaVersion = 0,
        bool isCallback = false,
        bool isCallbackError = false)
    {
        var result = ResultAggregate.Combine(
            ResultAggregate.FailureIf(string.IsNullOrEmpty(destiny), "[Destiny]", "Destiny is invalid"),
            ResultAggregate.FailureIf(string.IsNullOrEmpty(origin), "[Origin]", "Origin is invalid"),
            ResultAggregate.FailureIf(!isCallback && string.IsNullOrEmpty(originKey), "[OriginKey]", "Origin Key is invalid"),
            ResultAggregate.FailureIf(string.IsNullOrEmpty(entity), "[Entity]", "Entity is invalid"),
            ResultAggregate.FailureIf(string.IsNullOrEmpty(method), "[Method]", "Method is invalid"));
        if (result.IsFailure)
            return result.ConvertFailure<RegisterAsyncIntegrationEventCommand>();

        return new RegisterAsyncIntegrationEventCommand(
            destiny,
            destinyKey,
            origin,
            originKey,
            entity,
            method,
            correlationId,
            payload,
            schemaVersion,
            isCallback,
            aggregateKey,
            sapQueryAction,
            statusCode,
            version,
            isCallbackError);
    }
}