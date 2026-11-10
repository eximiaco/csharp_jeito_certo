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
    : IService<RouteIntegrationEventApplication>
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
                    kafkaMessage.TopicPartition.Partition.Value);
            }

            var route = await routingDataAccess.GetRouteWithoutRules(command.RouteId);
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
                await integrationEventsDataAccess.UpdateIntegrationProcess(integrationEvent.Value);
                routeIntegrationEventTelemetry.DispatcherNeedsRetry(integrationEvent.Value, dispatcherResult.Message);
                throw new RetryDispatcherException(
                    integrationEvent.Value.Entity.Destiny.Name!,
                    integrationEvent.Value.Entity.Entity,
                    integrationEvent.Value.Id);
            }

            integrationEvent.Value.RegisterSuccessOnDispatching();
            using var statusLog = LogContext.PushProperty(otelVariables.Status, integrationEvent.Value.Control.IntegrationStatus);

            await integrationEventsDataAccess.UpdateIntegrationProcess(integrationEvent.Value);
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
