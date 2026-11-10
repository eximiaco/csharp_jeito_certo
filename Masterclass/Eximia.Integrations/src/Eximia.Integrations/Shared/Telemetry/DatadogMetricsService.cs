using Microsoft.Extensions.Configuration;
using StatsdClient;
using ILogger = Serilog.ILogger;

namespace Eximia.Integrations.Shared.Telemetry;

public sealed class DatadogMetricsService : IDisposable
{
    private readonly DogStatsdService? _service;
    private readonly bool _initialized;
    private readonly ILogger _logger;

    public DatadogMetricsService(IConfiguration configuration, ILogger logger)
    {
        _initialized = false;
        _logger = logger;

        if (configuration.GetSection("OpenTelemetry") is var section && section.Exists())
        {
            var port = int.Parse(section["DatadogPort"] ?? "8125");
            var config = new StatsdConfig()
            {
                StatsdServerName = section["DatadogServer"],
                StatsdPort = port
            };

            _service = new DogStatsdService();
            if (_service.Configure(config))
                _initialized = true;
            else
                logger.Error("Cannot initialize DogstatsD.");
        }
        else
            logger.Error("Configuration for Datadog metrics not exists.");
    }

    public void SetGaugeMetric(string metricName, long value, string[] tags)
    {
        if (_initialized)
            _service!.Gauge(metricName, value, tags: tags);
    }

    public void SetCount(string metricName, long value, string[] tags)
    {
        if (_initialized)
            _service!.Counter(metricName, value, tags: tags);
    }

    public void SetServiceCheck(string name, Status status, string[] tags)
    {
        if (_initialized)
            _service!.ServiceCheck(name, status, tags: tags);
    }

    public void Dispose()
    {
        _service!.Dispose();
    }
}
