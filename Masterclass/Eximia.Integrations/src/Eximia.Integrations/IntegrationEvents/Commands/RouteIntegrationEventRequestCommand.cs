using Silverback.Messaging.Messages;

namespace Eximia.Integrations.IntegrationEvents.Commands;

public class RouteIntegrationEventRequestCommand(
    Guid id,
    int intId,
    string key,
    string topic,
    string origin,
    string destiny,
    string entity,
    string controlCode,
    bool isCallback,
    long routeId)
{
    [Header("x-integration-id")]
    public Guid Id { get; } = id;
    public int IntId { get; } = intId;
    public string Key { get; } = key;
    
    [Header("x-integration-topic")]
    public string Topic { get; } = topic;
    
    [Header("x-integration-origin")]
    public string Origin { get; } = origin;
    
    [Header("x-integration-destiny")]
    public string Destiny { get; } = destiny;
    
    [Header("x-integration-entity")]
    public string Entity { get; } = entity;
    public string ControlCode { get; } = controlCode;
    public bool IsCallback { get; } = isCallback;
    public long RouteId { get; } = routeId;

    public static RouteIntegrationEventRequestCommand Create(
        IntegrationEvent integrationEvent,
        string origin,
        string destiny,
        string entity,
        long routeId)
    {
        return new RouteIntegrationEventRequestCommand(
            integrationEvent.UniqueId,
            integrationEvent.Id,
            integrationEvent.Entity.AggregateKey,
            integrationEvent.Entity.DestinationQueue,
            origin,
            destiny,
            entity,
            integrationEvent.ControlCode,
            integrationEvent.Control.IsCallback,
            routeId);
    }
}
