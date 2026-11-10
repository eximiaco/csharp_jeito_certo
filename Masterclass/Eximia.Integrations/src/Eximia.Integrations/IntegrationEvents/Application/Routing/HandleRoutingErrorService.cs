using Silverback.Messaging.Publishing;
using Eximia.Integrations.Shared;
using Eximia.Integrations.DestinationsErrorsConfiguration;
using Eximia.Integrations.Events;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.IntegrationEvents.Telemetry;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Routing.Exceptions;

namespace Eximia.Integrations.IntegrationEvents.Application.Routing;

public class HandleRoutingErrorService(
    IntegrationEventsDataAccess integrationEventsDataAccess,
    IRouteIntegrationEventTelemetry routeIntegrationEventTelemetry,
    CheckIfErrorStatusCodeIsAllowedService checkIfErrorStatusCodeIsAllowedService,
    IPublisher publisher,
    PublishPlataformEventService publishPlataformEventService)
{
    public async Task HandleAsync(
        RouteIntegrationEventRequestCommand command,
        IntegrationEvent integrationEvent,
        DispatcherResult dispatcherResult,
        CancellationToken cancellationToken)
    {
        integrationEvent.RegisterFailure(dispatcherResult.Body);
        await integrationEventsDataAccess.UpdateIntegrationProcess(integrationEvent, cancellationToken);
        await publishPlataformEventService.PublishIntegrationEventErrorAsync(integrationEvent);
        routeIntegrationEventTelemetry.ErrorOnDispatcher(integrationEvent, dispatcherResult);

        var needRetry = await checkIfErrorStatusCodeIsAllowedService
            .NeedRetryAsync(command.Destiny, dispatcherResult.Body, dispatcherResult.StatusCode, cancellationToken)
            .ConfigureAwait(false);

        if (needRetry)
        {
            throw new FailureDispatcherException(
                integrationEvent.Entity.Destiny.Name!,
                integrationEvent.Entity.Entity,
                integrationEvent.Id,
                dispatcherResult.Body,
                dispatcherResult.StatusCode);
        }

        if (integrationEvent.Control.IsCallbackError)
            return;

        ProcessIntegrationEventCallbackCommand callbackMessage = new(integrationEvent.Id, dispatcherResult.StatusCode, command.Key);
        await publisher.PublishAsync(callbackMessage).WaitAsync(cancellationToken);
    }
}
