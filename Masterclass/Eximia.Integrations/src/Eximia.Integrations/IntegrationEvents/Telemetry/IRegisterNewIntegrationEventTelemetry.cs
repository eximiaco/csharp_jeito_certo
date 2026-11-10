using CSharpFunctionalExtensions;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.IntegrationEvents.Telemetry;

public interface IRegisterNewIntegrationEventTelemetry
{
    void IntegrationEventReceived(RegisterAsyncIntegrationEventCommand command);
    ResultAggregate RouteNotFound(RegisterAsyncIntegrationEventCommand command);
    void RouteFounded(Route route);
    ResultAggregate SchemaNotFound(RegisterAsyncIntegrationEventCommand command);
    ResultAggregate FailOnCreateIntegrationEvent(RegisterAsyncIntegrationEventCommand command, Dictionary<string, Result> errors);
    ResultAggregate FailOnPolicyRules(RegisterAsyncIntegrationEventCommand command, Dictionary<string, Result> errors);
    void IntegrationEventCreated(IntegrationEvent integrationEvent);
    void IntegrationEventPublish(IntegrationEvent integrationEvent);
    void IntegrationEventRequestProcessed();
    void FatalErrorCreatingIntegrationEvent(Exception exception);
}