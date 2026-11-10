namespace Eximia.Integrations.Shared.OpenTelemetry;

public record TelemetrySettings(string ServiceName, string ServiceVersion, TelemetryExporter Exporter);

public record TelemetryExporter(string Type, string Endpoint);
