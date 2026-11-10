using Serilog;
using Silverback.Messaging.Broker;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Events;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Routing.Exceptions;

namespace Eximia.Integrations.IntegrationEvents.Application.Routing;

public class RouteIntegrationEventApplication(
    IntegrationEventsDataAccess integrationEventsDataAccess,
    RoutingService routingService,
    HandleRoutingErrorService handleRoutingErrorService,
    PublishPlataformEventService publishPlataformEventService,
    RoutingDataAccess routingDataAccess,
    ILogger logger)
{
    public async Task RouteAsync(
        KafkaOffset? kafkaMessage,
        RouteIntegrationEventRequestCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            var integrationEvent = await integrationEventsDataAccess.GetAndSetEventToDispatch(command.Id, cancellationToken);
            if (integrationEvent.HasNoValue)
            {
                logger.Error("Integration event {IntegrationEventId} not found.", command.Id);
                return;
            }

            // O offset/partição passam a ser gravados no consumo (o OutboxWorker padrão do Silverback não expõe o ack do produtor).
            if (kafkaMessage is not null)
            {
                await integrationEventsDataAccess.UpdateKafkaOffsetAsync(
                    integrationEvent.Value.UniqueId,
                    kafkaMessage.Offset.Value,
                    kafkaMessage.TopicPartition.Partition.Value,
                    cancellationToken);
            }

            var route = await routingDataAccess.GetRouteWithoutRules(command.RouteId, cancellationToken);
            if (route.HasNoValue)
            {
                logger.Error("Route {RouteId} not found for integration event {IntegrationEventId}.", command.RouteId, command.Id);
                return;
            }

            var eventDate = DateTime.UtcNow;
            var dispatcherResult = await routingService.SendIntegrationEvent(integrationEvent.Value, route.Value, cancellationToken);
            if (dispatcherResult.IsFailure)
            {
                await handleRoutingErrorService.HandleAsync(command, integrationEvent.Value, dispatcherResult, cancellationToken);
                return;
            }

            if (dispatcherResult.IsRetry)
            {
                integrationEvent.Value.RegisterRetry(dispatcherResult.Message);
                await integrationEventsDataAccess.UpdateIntegrationProcess(integrationEvent.Value, cancellationToken);
                logger.Warning("Integration event {IntegrationEventId} needs retry. | Error: {Error}", command.Id, dispatcherResult.Message);
                throw new RetryDispatcherException(
                    integrationEvent.Value.Entity.Destiny.Name!,
                    integrationEvent.Value.Entity.Entity,
                    integrationEvent.Value.Id);
            }

            integrationEvent.Value.RegisterSuccessOnDispatching();

            // O destino JÁ recebeu o evento. Gravar o resultado com o token do consumer faria um cancelamento
            // (shutdown) deixar o evento "não confirmado" e a reentrega duplicaria a entrega: por isso, aqui, um
            // token PRÓPRIO e curto, e não o do chamador.
            using var persist = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await integrationEventsDataAccess.UpdateIntegrationProcess(integrationEvent.Value, persist.Token);
            await publishPlataformEventService.PublishIntegrationEventSentAsync(
                integrationEvent.Value.ControlCode,
                integrationEvent.Value.Control.IsCallback,
                eventDate);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // SHUTDOWN: cancelamento não é falha de entrega. Relança como está: o consumer não confirma o
            // offset, o evento é reentregue depois do restart e nenhum callback de erro sai.
            throw;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "Integration event {IntegrationEventId} failed. | Error: {Error}", command.Id, ex.Message);
            throw new FailureDispatcherException(string.Empty, string.Empty, 0, ex.Message, ex);
        }
    }
}
