using Silverback.Messaging.Broker;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Routing;

namespace Eximia.Integrations.IntegrationEvents.Telemetry;

public interface IRouteIntegrationEventTelemetry : IDisposable
{
    void IntegrationEventConsumed(RouteIntegrationEventRequestCommand command, KafkaOffset kafkaMessage);
    void FailOnRetrieveIntegrationEvent(RouteIntegrationEventRequestCommand command);
    void FailOnRetrieveRoute(RouteIntegrationEventRequestCommand command);
    void IntegrationEventLoaded(IntegrationEvent integrationEvent);
    void ErrorOnDispatcher(IntegrationEvent integrationEvent, DispatcherResult dispatcherResult);
    void DispatcherNeedsRetry(IntegrationEvent integrationEvent, string message);
    void IntegrationEventDispatched(IntegrationEvent integrationEvent);
    void CriticalFail(Exception exception, RouteIntegrationEventRequestCommand messageMessage);
}