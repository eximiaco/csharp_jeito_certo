namespace Eximia.Integrations.Events;

public record IntegrationEventErrorEvent(string ErrorMessage, bool IsCallback);