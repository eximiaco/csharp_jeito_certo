using System.Diagnostics.Metrics;
using Serilog;
using Eximia.Integrations.Shared.Telemetry;

namespace Eximia.Integrations.Outbox;

public sealed class OutboxMetrics : IDisposable
{
    private readonly DatadogMetricsService _datadogMetricsService;
    private readonly ILogger _logger;
    private readonly Meter _meter;
    private string _env = String.Empty;
    
    public OutboxMetrics(DatadogMetricsService datadogMetricsService, ILogger logger)
    {
        _datadogMetricsService = datadogMetricsService;
        _logger = logger;
        _meter = new Meter(MeterName);
        PublishedMessagesCount = _meter.CreateCounter<int>("integration_platform.outbox_published_count");
        QueuedActiveMessagesCount = _meter.CreateUpDownCounter<int>("integration_platform.outbox_active_queued");
        _measurements = new List<OutboxMeasurement>();
        _env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
    }

    public static readonly string MeterName = "integration_platform.outbox";
    
    private Counter<int> PublishedMessagesCount { get; }
    private UpDownCounter<int> QueuedActiveMessagesCount { get; }
    private IList<OutboxMeasurement> _measurements;
    
    public void AddPublishedMessages(string? origin, string? destiny, string? entity)
    {
        PublishedMessagesCount.Add(1,
            new KeyValuePair<string, object?>("origin", origin),
            new KeyValuePair<string, object?>("destiny", destiny),
            new KeyValuePair<string, object?>("entity", entity),
            new KeyValuePair<string, object?>("env", _env));
        
        if(QueuedActiveMessagesCount.Enabled)
            QueuedActiveMessagesCount.Add(-1,
                new KeyValuePair<string, object?>("origin", origin),
                new KeyValuePair<string, object?>("destiny", destiny),
                new KeyValuePair<string, object?>("entity", entity),
                new KeyValuePair<string, object?>("env", _env));
    }

    public void EnqueueMessage(string origin, string destiny, string entity, string? squad, bool isCallback)
    {
        QueuedActiveMessagesCount.Add(1,
            new KeyValuePair<string, object?>("origin", origin),
            new KeyValuePair<string, object?>("destiny", destiny),
            new KeyValuePair<string, object?>("entity", entity),
            new KeyValuePair<string, object?>("squad", squad),
            new KeyValuePair<string, object?>("is_callback", isCallback),
            new KeyValuePair<string, object?>("env", _env));
    }

    public void UpdateTotalQueue(IEnumerable<OutboxMeasurement> measurements)
    {
        foreach (var measurement in _measurements)
            measurement.Reset();

        foreach (var measurement in measurements)
        {
            var existsMeasurement = _measurements
                .FirstOrDefault(c => c.Origin == measurement.Origin &&
                                     c.Destiny == measurement.Destiny &&
                                     c.Entity == measurement.Entity);
            if (existsMeasurement == null)
                _measurements.Add(measurement);
            else
                existsMeasurement.Update(measurement.Total);
        }
        
        foreach (var measurement in _measurements)
        {
            _datadogMetricsService.SetGaugeMetric(
                "integration_platform.outbox_queued_datadog_count",
                measurement.Total,
                new []
                {
                    $"origin:{measurement.Origin}",
                    $"destiny:{measurement.Destiny}",
                    $"entity:{measurement.Entity}",
                    $"env:{_env}"
                });
        }
    }
    
    public void Dispose()
    {
        _meter.Dispose();
        _datadogMetricsService.Dispose();
    }
}
