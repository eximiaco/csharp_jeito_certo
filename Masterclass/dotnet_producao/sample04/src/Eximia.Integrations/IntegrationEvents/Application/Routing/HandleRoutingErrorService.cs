using Serilog;
using Silverback.Messaging.Publishing;
using Eximia.Integrations.Shared;
using Eximia.Integrations.DestinationsErrorsConfiguration;
using Eximia.Integrations.Events;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Routing.Exceptions;

namespace Eximia.Integrations.IntegrationEvents.Application.Routing;

public class HandleRoutingErrorService(
    IntegrationEventsDataAccess integrationEventsDataAccess,
    CheckIfErrorStatusCodeIsAllowedService checkIfErrorStatusCodeIsAllowedService,
    IPublisher publisher,
    PublishPlataformEventService publishPlataformEventService,
    ILogger logger)
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
        logger.Error(
            "Error dispatching integration event {IntegrationEventId} to {Destiny}. | StatusCode: {StatusCode}. Body: {Body}",
            integrationEvent.Id, integrationEvent.Entity.Destiny.Name, dispatcherResult.StatusCode, dispatcherResult.Body);

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
        await publisher.PublishAsync(callbackMessage, cancellationToken);
    }
}
