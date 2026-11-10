using Polly;

namespace Eximia.Integrations.Routing.Telemetry;

public static class TelemetryExtensions
{
    private const string OpenTelemetryKey = "OpenTelemetry";
    
    public static Context WithOpenTelemetry(this Context context, IDispatcherEventTelemetry openTelemetryService)
    {
        context[OpenTelemetryKey] = openTelemetryService;
        return context;
    }
    
    public static IDispatcherEventTelemetry? GetOpenTelemetryService(this Context context)
    {
        if (context.TryGetValue(OpenTelemetryKey, out var openTelemetryService))
        {
            return openTelemetryService as IDispatcherEventTelemetry;
        }
        return null;
    }

    public static string AnonimateToke(this string accessToken)
    {
        return $"{accessToken.Substring(0, 4)}...{accessToken.Substring(accessToken.Length - 4, 4)}";
    }
}