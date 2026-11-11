namespace Eximia.Integrations.Events;

public record IntegrationEventReceivedEvent(
    string Entity,
    string? Origin,
    string? Destiny,
    string? OriginId,
    string? DestinyId,
    string Action,
    bool NeedCallback);
