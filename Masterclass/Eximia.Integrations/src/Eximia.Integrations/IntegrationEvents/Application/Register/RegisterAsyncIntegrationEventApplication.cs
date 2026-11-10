using Serilog.Context;
using Silverback.Messaging.Publishing;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Extensions;
using Eximia.Integrations.DomainServices;
using Eximia.Integrations.Events;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.IntegrationEvents.Services;
using Eximia.Integrations.IntegrationEvents.Telemetry;
using Eximia.Integrations.Outbox;
using Eximia.Integrations.Outbox.EntityFramework;
using Eximia.Integrations.Routing;

namespace Eximia.Integrations.IntegrationEvents.Application.Register;

public sealed class RegisterAsyncIntegrationEventApplication(
    JsonSchemaService jsonSchemaService,
    IntegrationEventPolicyEngine integrationEventPolicyEngine,
    IntegrationEventsDataAccess integrationEventsDataAccess,
    RouteValidator routeValidator,
    RoutingDataAccess routingDataAccess,
    IntegrationEventActionsEngine integrationEventActionsEngine,
    IPublisher publisher,
    IRegisterNewIntegrationEventTelemetry registerNewIntegrationEventTelemetry,
    EximiaIntegrationDbContext eximiaIntegrationDbContext,
    PublishPlataformEventService publishPlataformEventService,
    OtelVariables otelVariables,
    PayloadService payloadService)
    : IService<RegisterAsyncIntegrationEventApplication>
{
    public async Task<ResultAggregate> HandleV1(RegisterAsyncIntegrationEventCommand command, CancellationToken cancellationToken = default)
    {
        if (!await routeValidator.RouteIsValid(command.Destiny, command.Entity))
            return ResultAggregate.Failure("Destiny", "No route for destiny and entity");

        var integrationEvent = jsonSchemaService.CreateFrom(
            command.Destiny,
            command.DestinyKey,
            command.Origin,
            command.OriginKey,
            command.Entity,
            command.Method,
            command.CorrelationId,
            command.IsCallback,
            command.SapQueryAction,
            command.StatusCode,
            command.Version);
        if (integrationEvent.IsFailure)
            return integrationEvent;

        await integrationEventsDataAccess.InsertAsync(integrationEvent.Value, cancellationToken);
        return ResultAggregate.Success();
    }

    public async Task<ResultAggregate> HandleV2(RegisterAsyncIntegrationEventCommand command, CancellationToken cancellationToken = default)
    {
        try
        {
            registerNewIntegrationEventTelemetry.IntegrationEventReceived(command);

            var route = await routingDataAccess.GetRoute(command.Entity, command.Destiny).ConfigureAwait(false);
            if (route.HasNoValue)
                return registerNewIntegrationEventTelemetry.RouteNotFound(command);
            registerNewIntegrationEventTelemetry.RouteFounded(route.Value);

            using var routeLog = LogContext.PushProperty(otelVariables.RouteId, route.Value.Id);

            var payload = command.Payload.ToStringJson();
            var payloadFile = command.PayloadFile;
            var payloadFileId = command.FileId;
            if (!command.IsPayloadLink)
            {
                payloadFileId = Guid.NewGuid();

                var uploadResult = await payloadService.UploadAsync(
                    payload,
                    payloadFileId,
                    cancellationToken);

                payload = uploadResult.NewPayload;
                payloadFile = uploadResult.FileName;
            }

            var integrationEvent = jsonSchemaService.CreateFrom(
                command,
                route.Value,
                isPayloadLink: true,
                payloadFile,
                payloadFileId);

            if (integrationEvent.IsFailure)
                return registerNewIntegrationEventTelemetry.FailOnCreateIntegrationEvent(command, integrationEvent.NestedResults);

            using var uniqueIdLog = LogContext.PushProperty(otelVariables.IntegrationUniqueId, integrationEvent.Value.UniqueId);
            using var controlCodeLog = LogContext.PushProperty(otelVariables.ControlCode, integrationEvent.Value.ControlCode);
            using var statusLog = LogContext.PushProperty(otelVariables.Status, integrationEvent.Value.Control.IntegrationStatus);

            var validationResult = await integrationEventPolicyEngine.Execute(integrationEvent.Value, cancellationToken);
            if (validationResult.IsFailure)
                return registerNewIntegrationEventTelemetry.FailOnPolicyRules(command, validationResult.NestedResults);

            // Integration event + mensagens do outbox do Silverback são confirmados na mesma transação.
            await using (var outboxTransaction = await OutboxTransaction.BeginAsync(eximiaIntegrationDbContext, publisher, cancellationToken))
            {
                await eximiaIntegrationDbContext.IntegrationEvents.AddAsync(integrationEvent.Value, cancellationToken);
                registerNewIntegrationEventTelemetry.IntegrationEventCreated(integrationEvent.Value);

                var message = RouteIntegrationEventRequestCommand.Create(
                    integrationEvent.Value,
                    command.Origin,
                    command.Destiny,
                    command.Entity,
                    route.Value.Id);
                await publisher.PublishAsync(message);
                await publishPlataformEventService.PublishIntegrationEventReceivedAsync(integrationEvent.Value);

                await eximiaIntegrationDbContext.SaveChangesAsync(cancellationToken);
                await outboxTransaction.CommitAsync(cancellationToken);
            }

            using var eventIdLog = LogContext.PushProperty(otelVariables.IntegrationId, integrationEvent.Value.Id);
            await integrationEventActionsEngine.Execute(integrationEvent.Value, cancellationToken);

            registerNewIntegrationEventTelemetry.IntegrationEventPublish(integrationEvent.Value);
            registerNewIntegrationEventTelemetry.IntegrationEventRequestProcessed();

            return ResultAggregate.Success();
        }
        catch (Exception ex)
        {
            registerNewIntegrationEventTelemetry.FatalErrorCreatingIntegrationEvent(ex);
            throw;
        }
    }
}
