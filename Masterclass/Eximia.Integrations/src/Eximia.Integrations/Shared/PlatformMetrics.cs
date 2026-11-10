using System.Diagnostics.Metrics;
using Serilog;
using StatsdClient;
using Eximia.Integrations.Shared.Telemetry;

namespace Eximia.Integrations.Shared;

public class PlatformMetrics : IDisposable
{
    private readonly ILogger _logger;
    private readonly DatadogMetricsService _metricsService;
    private readonly Meter _meter;
    private string _env;

    public PlatformMetrics(ILogger logger, DatadogMetricsService metricsService)
    {
        _logger = logger;
        _metricsService = metricsService;
        _meter = new Meter(MeterName);
        IntegrationReceivedCount = _meter.CreateCounter<long>("integration_platform.received_count_old", "hits");
        IntegrationSuccessCount = _meter.CreateCounter<long>("integration_platform.dispatcher_processed_count", "hits");
        IntegrationFailureCount = _meter.CreateCounter<long>("integration_platform.dispatcher_failure_count", "hits");
        IntegrationDestinyFailureCount = _meter.CreateCounter<long>("integration_platform.dispatcher_destiny_failure_count", "hits");
        _env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

    }

    public static readonly string MeterName = "integration_platform";
    private Counter<long> IntegrationReceivedCount { get; }
    private Counter<long> IntegrationSuccessCount { get; }
    private Counter<long> IntegrationFailureCount { get; }
    private Counter<long> IntegrationDestinyFailureCount { get; }

    public void IntegrationAsyncRequestCreated(string? origin, string? destiny, string? entity, string? squad, bool isCallback)
    {
        _logger.Information("Integration Received Metrics recorded. {origin} | {destiny} | {entity} | {squad} | {env}",
            origin, destiny, entity, squad, _env);

        _metricsService.SetCount("integration_platform.event_received_count", 1,
        [
            $"origin:{origin}",
            $"destiny:{destiny}",
            $"entity:{entity}",
            $"squad:{squad}",
            $"is_callback:{isCallback}",
            $"env:{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}"
        ]);
    }

    public void SuccessIntegration(PlatformMeasurement measurement)
    {
        _metricsService.SetCount("integration_platform.event_completed_count", 1,
            [
                $"origin:{measurement.Origin}",
                $"destiny:{measurement.Destiny}",
                $"entity:{measurement.Entity}",
                $"squad:{measurement.Squad}",
                $"is_callback:{measurement.IsCallback}",
                $"env:{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}"
            ]);
    }

    public void IntegrationFailure(PlatformMeasurement measurement)
    {
        _metricsService.SetCount("integration_platform.error_count", 1,
            [
                $"origin:{measurement.Origin}",
                $"destiny:{measurement.Destiny}",
                $"entity:{measurement.Entity}",
                $"squad:{measurement.Squad}",
                $"is_callback:{measurement.IsCallback}",
                $"env:{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}"
            ]);
    }

    public void DestinationFailure(PlatformMeasurement measurement)
    {
        _metricsService.SetCount("integration_platform.destiny_error_count", 1,
            [
                $"origin:{measurement.Origin}",
                $"destiny:{measurement.Destiny}",
                $"entity:{measurement.Entity}",
                $"squad:{measurement.Squad}",
                $"is_callback:{measurement.IsCallback}",
                $"env:{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}"
            ]);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
