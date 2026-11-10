using System.Diagnostics;

namespace Eximia.Integrations.Shared.OpenTelemetry;

public class OtelTracingService : IDisposable
{
    public ActivitySource ActivitySource { get; }

    public OtelTracingService(TelemetrySettings? settings) => ActivitySource = new(settings!.ServiceName);

    public void Dispose()
    {
        ActivitySource.Dispose();
    }
}
