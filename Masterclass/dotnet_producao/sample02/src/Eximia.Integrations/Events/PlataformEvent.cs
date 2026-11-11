namespace Eximia.Integrations.Events;

public record PlataformEvent(string Name, Guid ControlCode, DateTime Timestamp, object Data);