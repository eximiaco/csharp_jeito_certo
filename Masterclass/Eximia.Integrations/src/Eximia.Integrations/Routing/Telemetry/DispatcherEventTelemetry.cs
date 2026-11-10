using Serilog;
using Serilog.Context;
using Eximia.Integrations.Shared.OpenTelemetry;
using Eximia.Integrations.IntegrationEvents;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.Routing.Telemetry;

public sealed class DispatcherEventTelemetry : IDispatcherEventTelemetry
{
    private IOtelTelemetry? _activity = null!;
    private readonly ITelemetryFactory _telemetryFactory;
    private readonly PlatformMetrics _platformMetrics;
    private readonly OtelVariables _otelVariables;
    private readonly ILogger _logger;

    public DispatcherEventTelemetry(
        ITelemetryFactory telemetryFactory,
        PlatformMetrics platformMetrics,
        OtelVariables otelVariables,
        ILogger logger)
    {
        _telemetryFactory = telemetryFactory;
        _platformMetrics = platformMetrics;
        _otelVariables = otelVariables;
        _logger = logger;
    }

    public void Dispose()
    {
        _activity?.Dispose();
    }

    public void RequestFinished()
    {
        _activity?.SetSuccess("Sucess");
        //_platformMetrics.DispatchSuccess();
    }

    public void IntegrationEventLoaded(string serviceName, IntegrationEvent integrationEvent, Route route)
    {
        _activity = _telemetryFactory.Create(serviceName);
        _activity?
            .AddTag(_otelVariables.Destiny, integrationEvent.Entity.Destiny.Name)
            .AddTag(_otelVariables.Origin, integrationEvent.Entity.Origin.Name)
            .AddTag(_otelVariables.Entity, integrationEvent.Entity.Entity)
            .AddTag(_otelVariables.AggregateKey, integrationEvent.Entity.AggregateKey)
            .AddTag("DestinyURI", integrationEvent.Control.IsCallbackError ? route.CallbackErrorUri : route.Uri);
    }

    public void MakingCallToDestiny()
    {
        _activity?.AddEvent("Making call");
        //_platformMetrics.Dispacthing();
    }

    public void MakeRetry(int retryCount, Exception resultException, string body, int statusCode)
    {
        _activity?
            .AddTag(_otelVariables.ErrorStatusCode, statusCode)
            .AddTag(_otelVariables.ErrorBody, body)
            .AddEvent($"Retry {retryCount} started")
            .AddException(body, resultException);

        using (LogContext.PushProperty(_otelVariables.ErrorStatusCode, statusCode))
        using (LogContext.PushProperty(_otelVariables.ErrorBody, body))
            _logger.Error(resultException, $"Retry {retryCount}.");
    }

    public void RequestFailed(Exception resultException, string body, int statusCode)
    {
        _activity?
            .AddTag(_otelVariables.ErrorStatusCode, statusCode)
            .AddTag(_otelVariables.ErrorBody, body)
            .AddException(body, resultException)
            .SetError("Fail on dispatcher");
        
        using (LogContext.PushProperty(_otelVariables.ErrorStatusCode, statusCode))
        using (LogContext.PushProperty(_otelVariables.ErrorBody, body))
        _logger.Error(resultException, "Request failed.");
    }

    public void UnAuthorizeCall()
    {
        _activity?.AddEvent("New Token required");
    }
}
