using Eximia.Integrations.Shared.OpenTelemetry;

namespace Eximia.Integrations.Shared;

public interface ITelemetryService
{
    void ExceptionFailure(Exception exception, string message);
}

public sealed class TelemetryService: ITelemetryService
{
    private IOtelTelemetry _telemetry;

    public TelemetryService(ITelemetryFactory telemetryFactory)
    {
        _telemetry = telemetryFactory.GetCurrentActivity();

    }
    
    public void ExceptionFailure(Exception exception, string message)
    {
        _telemetry
            .AddTag("Exception.Message", exception.Message)
            .AddTag("Exception.StackTrace", exception.StackTrace)
            .AddException(message, exception);
    }
}
