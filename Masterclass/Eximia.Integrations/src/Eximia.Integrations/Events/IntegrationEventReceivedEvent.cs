namespace Eximia.Integrations.Events;

public record IntegrationEventReceivedEvent(
    string Entity,
    string? Squad,
    string? Origin,
    string? Destiny,
    string? OriginId,
    string? DestinyId,
    string Action,
    bool NeedCallback);
