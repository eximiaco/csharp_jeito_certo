namespace Eximia.Integrations.Events;

public static class PlataformEventType
{
    public static string IntegrationEventReceived => "IntegrationEventReceived";
    public static string IntegrationEventSent => "IntegrationEventSent";
    public static string IntegrationEventError => "IntegrationEventError";
    public static string Rebuild => "Rebuild";
}