using System.Diagnostics;
using Serilog;

namespace Eximia.Integrations.Shared.OpenTelemetry;

public interface ITelemetryFactory
{
    IOtelTelemetry? Create(string spanName);
    IOtelTelemetry CreateLog();
    IOtelTelemetry GetCurrentActivity();
    IOtelTelemetry GetFromActivity(Activity activity);
}

public class TelemetryFactory : ITelemetryFactory, IService<TelemetryFactory>
{
    private readonly OtelTracingService _otelTracingService;
    private readonly ILogger _logger;

    public TelemetryFactory(
        OtelTracingService otelTracingService,
        ILogger logger)
    {
        _otelTracingService = otelTracingService;
        _logger = logger;
    }
        
    public IOtelTelemetry? Create(string spanName)
    {
        return new OtelTelemetry(
            _otelTracingService.ActivitySource.StartActivity(spanName),
            _logger);
    }
    
    public IOtelTelemetry CreateLog()
    {
        return new OtelTelemetry(
            null,
            _logger);
    }

    public IOtelTelemetry GetCurrentActivity()
    {
        return new OtelTelemetry(
            Activity.Current, 
            _logger);
    }

    public IOtelTelemetry GetFromActivity(Activity activity)
    {
        return new OtelTelemetry(
            activity, 
            _logger);
    }
    
}
