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
        await integrationEventsDataAccess.UpdateIntegrationProcess(integrationEvent);
        await publishPlataformEventService.PublishIntegrationEventErrorAsync(integrationEvent);
        logger.Error(
            "Error dispatching integration event {IntegrationEventId} to {Destiny}. | StatusCode: {StatusCode}. Body: {Body}",
            integrationEvent.Id, integrationEvent.Entity.Destiny.Name, dispatcherResult.StatusCode, dispatcherResult.Body);

        // PROBLEMA: GetResult() bloqueia a thread esperando o banco. Com o destino fora do ar, todas as mensagens caem aqui juntas.
        var needRetry = checkIfErrorStatusCodeIsAllowedService
            .NeedRetryAsync(command.Destiny, dispatcherResult.Body, dispatcherResult.StatusCode, cancellationToken)
            .GetAwaiter().GetResult();

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
        // PROBLEMA: PublishAsync aceita o token e ele não é passado. O envio não pode ser interrompido.
        await publisher.PublishAsync(callbackMessage);
    }
}
