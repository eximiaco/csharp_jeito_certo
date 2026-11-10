using Serilog.Context;
using Silverback.Messaging.Broker;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Events;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.IntegrationEvents.Telemetry;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Routing.Exceptions;

namespace Eximia.Integrations.IntegrationEvents.Application.Routing;

public class RouteIntegrationEventApplication(
    IntegrationEventsDataAccess integrationEventsDataAccess,
    RoutingService routingService,
    IRouteIntegrationEventTelemetry routeIntegrationEventTelemetry,
    HandleRoutingErrorService handleRoutingErrorService,
    PublishPlataformEventService publishPlataformEventService,
    OtelVariables otelVariables,
    RoutingDataAccess routingDataAccess)
{
    public async Task RouteAsync(
        KafkaOffset? kafkaMessage,
        RouteIntegrationEventRequestCommand command,
        CancellationToken cancellationToken)
    {
        try
        {
            routeIntegrationEventTelemetry.IntegrationEventConsumed(command, kafkaMessage!);

            var integrationEvent = await integrationEventsDataAccess.GetAndSetEventToDispatch(command.Id, cancellationToken);
            if (integrationEvent.HasNoValue)
            {
                routeIntegrationEventTelemetry.FailOnRetrieveIntegrationEvent(command);
                return;
            }

            using var eventIdLog = LogContext.PushProperty(otelVariables.IntegrationId, integrationEvent.Value.Id);
            using var destinyKeyLog = LogContext.PushProperty(otelVariables.DestinyKey, integrationEvent.Value.Entity.Destiny.Key);
            using var originKeyLog = LogContext.PushProperty(otelVariables.OriginKey, integrationEvent.Value.Entity.Origin.Key);
            using var aggregateKeyLog = LogContext.PushProperty(otelVariables.AggregateKey, integrationEvent.Value.Entity.AggregateKey);
            using var isCallbackLog = LogContext.PushProperty(otelVariables.IsCallback, integrationEvent.Value.Control.IsCallback);
            using var isCallbackErrorLog = LogContext.PushProperty(otelVariables.IsCallbackError, integrationEvent.Value.Control.IsCallbackError);

            routeIntegrationEventTelemetry.IntegrationEventLoaded(integrationEvent.Value);

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
                routeIntegrationEventTelemetry.FailOnRetrieveRoute(command);
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
                routeIntegrationEventTelemetry.DispatcherNeedsRetry(integrationEvent.Value, dispatcherResult.Message);
                throw new RetryDispatcherException(
                    integrationEvent.Value.Entity.Destiny.Name!,
                    integrationEvent.Value.Entity.Entity,
                    integrationEvent.Value.Id);
            }

            integrationEvent.Value.RegisterSuccessOnDispatching();
            using var statusLog = LogContext.PushProperty(otelVariables.Status, integrationEvent.Value.Control.IntegrationStatus);

            // O destino JÁ recebeu o evento. Gravar o resultado com o token do consumer faria um cancelamento
            // (shutdown) deixar o evento "não confirmado" e a reentrega duplicaria a entrega: por isso, aqui, um
            // token PRÓPRIO e curto, e não o do chamador.
            using var persist = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await integrationEventsDataAccess.UpdateIntegrationProcess(integrationEvent.Value, persist.Token);
            await publishPlataformEventService.PublishIntegrationEventSentAsync(
                integrationEvent.Value.ControlCode,
                integrationEvent.Value.Control.IsCallback,
                eventDate);

            routeIntegrationEventTelemetry.IntegrationEventDispatched(integrationEvent.Value);
        }
        catch (Exception ex)
        {
            routeIntegrationEventTelemetry.CriticalFail(ex, command);
            throw new FailureDispatcherException(string.Empty, string.Empty, 0, ex.Message, ex);
        }
    }
}
