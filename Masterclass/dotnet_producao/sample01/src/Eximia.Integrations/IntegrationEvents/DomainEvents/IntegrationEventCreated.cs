using Silverback.Domain;
using Silverback.Messaging.Messages;

namespace Eximia.Integrations.IntegrationEvents.DomainEvents;

public record IntegrationEventCreated(
    Guid id,
    int intId,
    string key,
    string topic,
    string origin,
    string destiny,
    string entity,
    string controlCode) : IDomainEvent
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
    public object? Source { get => nameof(IntegrationEventCreated); set { } }
}