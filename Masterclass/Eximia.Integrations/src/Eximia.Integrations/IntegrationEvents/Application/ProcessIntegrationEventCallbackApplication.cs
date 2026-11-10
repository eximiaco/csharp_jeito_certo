using Serilog.Context;
using Silverback.Messaging.Messages;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Auth;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.IntegrationEvents.Services;
using Eximia.Integrations.IntegrationEvents.Telemetry.Callback;
using static Eximia.Integrations.IntegrationEvents.Services.SendIntegrationEventService;

namespace Eximia.Integrations.IntegrationEvents.Application;

public class ProcessIntegrationEventCallbackApplication(
    IntegrationEventsDataAccess integrationEventsDataAccess,
    SendIntegrationEventService sendIntegrationEventCallbackService,
    ICallbackEventTelemetry callbackEventTelemetry,
    AzureAdAuthenticationDecorator azureAdAuthenticationDecorator,
    OtelVariables otelVariables,
    SquadsSingleton squadsSingleton)
{
    public async Task ConsumeMessage(IInboundEnvelope<ProcessIntegrationEventCallbackCommand> message, CancellationToken cancellationToken)
    {
        var command = message.Message!;

        try
        {
            using var eventIdLog = LogContext.PushProperty(otelVariables.IntegrationId, command.Id);
            using var statusCodeLog = LogContext.PushProperty(otelVariables.CallbackErrorStatusCode, command.StatusCode);
            using var aggregateKeyLog = LogContext.PushProperty(otelVariables.AggregateKey, command.Key);

            callbackEventTelemetry.Started(command);

            var integrationEvent = await integrationEventsDataAccess.GetAsync(command.Id, cancellationToken);
            if (integrationEvent.HasNoValue)
            {
                callbackEventTelemetry.IntegrationEventNotFound(command);
                return;
            }

            using var destinyLog = LogContext.PushProperty(otelVariables.Destiny, integrationEvent.Value.Entity.Destiny.Name);
            using var destinyKeyLog = LogContext.PushProperty(otelVariables.DestinyKey, integrationEvent.Value.Entity.Destiny.Key);
            using var originLog = LogContext.PushProperty(otelVariables.Origin, integrationEvent.Value.Entity.Origin.Name);
            using var originKeyLog = LogContext.PushProperty(otelVariables.OriginKey, integrationEvent.Value.Entity.Origin.Key);
            using var isCallbackLog = LogContext.PushProperty(otelVariables.IsCallback, integrationEvent.Value.Control.IsCallback);
            using var isCallbackErrorLog = LogContext.PushProperty(otelVariables.IsCallbackError, integrationEvent.Value.Control.IsCallbackError);
            using var statusLog = LogContext.PushProperty(otelVariables.Status, integrationEvent.Value.Control.IntegrationStatus);
            using var controlCodeLog = LogContext.PushProperty(otelVariables.ControlCode, integrationEvent.Value.ControlCode);
            using var uniqueIdLog = LogContext.PushProperty(otelVariables.IntegrationUniqueId, integrationEvent.Value.UniqueId);
            using var entityLog = LogContext.PushProperty(otelVariables.Entity, integrationEvent.Value.Entity.Entity);
            using var squadLog = LogContext.PushProperty(otelVariables.Squad, squadsSingleton.GetByEntity(integrationEvent.Value.Entity.Entity));
            using var lastErrorLog = LogContext.PushProperty(otelVariables.CallbackErrorResponseBody, integrationEvent.Value.Retry.LastError);

            if (!integrationEvent.Value.Entity.CallbackRequired)
            {
                callbackEventTelemetry.CallbackNotNeeded(command);
                return;
            }

            callbackEventTelemetry.RegisteringIntegrationEvent(integrationEvent.Value);

            IntegrationEventRequest request = new(
                Metadata.CreateForErrorCallback(
                    origin: integrationEvent.Value.Entity.Destiny,
                    destiny: integrationEvent.Value.Entity.Origin,
                    entity: Entity.Create(
                        name: integrationEvent.Value.Entity.Entity,
                        action: integrationEvent.Value.Entity.Method,
                        aggregateKey: integrationEvent.Value.Entity.AggregateKey),
                    correlationKey: integrationEvent.Value.ControlCode,
                    statusCode: command.StatusCode),
                data: new CallbackPayload(integrationEvent.Value.Retry.LastError));

            var token = await azureAdAuthenticationDecorator.AuthenticateAsync(cancellationToken);
            if (token.IsFailure)
            {
                callbackEventTelemetry.ErrorOnAuthentication(token.Error);
                return;
            }

            var result = await sendIntegrationEventCallbackService.SendAsync(
                request,
                token.Value,
                cancellationToken);
            
            callbackEventTelemetry.Completed(result);
        }
        catch (Exception ex)
        {
            callbackEventTelemetry.ErrorOnCallback(ex, command);
            throw;
        }
    }
}
