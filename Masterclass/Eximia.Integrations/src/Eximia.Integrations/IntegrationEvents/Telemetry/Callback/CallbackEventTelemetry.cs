using Serilog;
using Eximia.Integrations.Shared.OpenTelemetry;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.IntegrationEvents.Telemetry.Callback
{
    public class CallbackEventTelemetry : ICallbackEventTelemetry
    {
        private IOtelTelemetry? _activity;
        private readonly PlatformMetrics _platformMetrics;
        private readonly OtelVariables _otelVariables;
        private readonly ILogger _logger;
        private bool _disposed = false;

        public CallbackEventTelemetry(
            ITelemetryFactory telemetryFactory,
            PlatformMetrics platformMetrics,
            OtelVariables otelVariables,
            ILogger logger)
        {
            _activity = telemetryFactory.Create("Callback-Error");
            _platformMetrics = platformMetrics;
            _otelVariables = otelVariables;
            _logger = logger;
        }

        public void CallbackNotNeeded(ProcessIntegrationEventCallbackCommand message)
        {
            _activity?.SetSuccess("Error callback not needed.");
            _logger.Information("Error callback not needed.");
            Dispose(true);
        }

        public void Completed(DispatcherResult dispatcherResult)
        {
            _activity?.SetSuccess("Error callback completed.");
            _logger.Information("Error callback completed.");
            Dispose(true);
        }

        public void IntegrationEventNotFound(ProcessIntegrationEventCallbackCommand message)
        {
            _activity?.SetError("Error callback integration event not found.");            
            _logger.Warning("Error callback integration event not found.");
            Dispose(true);
        }

        public void RegisteringIntegrationEvent(IntegrationEvent integrationEvent)
        {
            _activity?
                .AddTag(_otelVariables.Entity, integrationEvent.Entity.Entity)
                .AddTag(_otelVariables.Destiny, integrationEvent.Entity.Destiny)
                .AddTag(_otelVariables.Origin, integrationEvent.Entity.Origin)
                .AddTag(_otelVariables.ControlCode, integrationEvent.ControlCode)
                .AddTag(_otelVariables.CallbackErrorResponseBody, integrationEvent.Retry.LastError);

            _activity?.AddEvent("Error callback registering integration event.");            
            _logger.Information("Error callback registering integration event.");
        }

        public void RouteNotFound(string entity, string destiny)
        {
            _activity?
                .AddTag(_otelVariables.Entity, entity)
                .AddTag(_otelVariables.Destiny, destiny)
                .SetSuccess("Error callback route not found.");

            Dispose(true);
        }

        public void Started(ProcessIntegrationEventCallbackCommand message)
        {
            _activity?
                .AddTag(_otelVariables.IntegrationEventId, message.Id)
                .AddTag(_otelVariables.AggregateKey, message.Key)
                .AddTag(_otelVariables.CallbackErrorStatusCode, message.StatusCode);

            _logger.Information("Error callback started.");
        }

        public void ErrorOnAuthentication(string error)
        {
            _activity?
                .AddTag(_otelVariables.CallbackAuthErrorResponseBody, error)
                .SetError("Error on authentication.");

            _logger.Error("Error on authentication. |  Error: {error}", error);
            Dispose(true);
        }

        public void ErrorOnCallback(Exception exception, ProcessIntegrationEventCallbackCommand message)
        {
            _activity?.AddExceptionAndEvent("Callback event fail.", exception);
            _activity?.SetError("Error callback event fail.");
            _logger.Fatal(exception, exception.Message);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _activity!.Dispose();
                _activity = null;
            }

            _disposed = true;
        }
    }
}
