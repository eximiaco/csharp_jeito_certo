using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Routing;

namespace Eximia.Integrations.IntegrationEvents.Telemetry.Callback
{
    public interface ICallbackEventTelemetry : IDisposable
    {
        void Started(ProcessIntegrationEventCallbackCommand message);
        void IntegrationEventNotFound(ProcessIntegrationEventCallbackCommand message);
        void CallbackNotNeeded(ProcessIntegrationEventCallbackCommand message);
        void RouteNotFound(string entity, string destiny);
        void RegisteringIntegrationEvent(IntegrationEvent integrationEvent);
        void Completed(DispatcherResult dispatcherResult);
        void ErrorOnAuthentication(string error);
        void ErrorOnCallback(Exception exception, ProcessIntegrationEventCallbackCommand message);
    }
}
