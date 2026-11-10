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
using Eximia.Integrations.Routing.Rules;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.IntegrationEvents.Application.Register;

public class RegisterMultipleRoutesIntegrationEventApplication(
    JsonSchemaService jsonSchemaService,
    IntegrationEventPolicyEngine integrationEventPolicyEngine,
    RoutingDataAccess routingDataAccess,
    IntegrationEventActionsEngine integrationEventActionsEngine,
    IPublisher publisher,
    IRegisterNewIntegrationEventTelemetry registerNewIntegrationEventTelemetry,
    EximiaIntegrationDbContext eximiaIntegrationDbContext,
    PublishPlataformEventService publishPlataformEventService,
    RoutingRuleEngine routingRuleEngine,
    OtelVariables otelVariables,
    PayloadService uploadIntegrationEventPayloadService)
{
    public async Task<ResultAggregate> HandleV3(
        RegisterMultipleRoutesIntegrationEventCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Converter para comando compatível com telemetry
            var asyncCommand = RegisterAsyncIntegrationEventCommand.Create(
                command.Destiny,
                command.DestinyKey,
                command.Origin,
                command.OriginKey,
                command.Entity,
                command.Method,
                command.CorrelationId,
                command.Payload,
                command.AggregateKey,
                command.SapQueryAction,
                command.StatusCode,
                command.Version,
                command.SchemaVersion,
                command.IsCallback,
                command.IsCallbackError,
                command.IsPayloadLink,
                command.PayloadFile,
                command.FileId);

            if (asyncCommand.IsFailure)
                return ResultAggregate.Failure("Command", "Invalid command parameters");

            registerNewIntegrationEventTelemetry.IntegrationEventReceived(asyncCommand.Value);

            // 1. Buscar TODAS as rotas para origem + entidade
            var allRoutes = await routingDataAccess.GetRoutesByOriginAndEntityAsync(
                command.Origin,
                command.Entity,
                cancellationToken);

            var allRoutesList = allRoutes.ToList();
            if (allRoutesList.Count == 0)
                return registerNewIntegrationEventTelemetry.RouteNotFound(asyncCommand.Value);

            // 2. Filtrar rotas aplicáveis baseado nas regras
            var applicableRoutes = await routingRuleEngine.FilterApplicableRoutesAsync(
                allRoutesList,
                command.Payload,
                command.IntegrationEventId,
                cancellationToken);

            var applicableRoutesList = applicableRoutes.ToList();
            if (applicableRoutesList.Count == 0)
                return ResultAggregate.Failure("Routing", "No applicable routes found");

            var payload = command.Payload.ToStringJson();
            var payloadFile = command.PayloadFile;
            var payloadFileId = command.FileId;
            if (!command.IsPayloadLink)
            {
                payloadFileId = Guid.NewGuid();

                var uploadResult = await uploadIntegrationEventPayloadService.UploadAsync(
                    payload,
                    payloadFileId,
                    cancellationToken);

                payload = uploadResult.NewPayload;
                payloadFile = uploadResult.FileName;
            }

            // 3. Processar cada rota aplicável
            var processedEvents = new List<IntegrationEvent>();

            // Integration events + mensagens do outbox do Silverback são confirmados na mesma transação.
            await using (var outboxTransaction = await OutboxTransaction.BeginAsync(eximiaIntegrationDbContext, publisher, cancellationToken))
            {
            foreach (var route in applicableRoutesList)
            {
                using var routeLog = LogContext.PushProperty(otelVariables.RouteId, route.Id);
                using var destinyLog = LogContext.PushProperty(otelVariables.Destiny, route.Destination);

                // Converter para comando compatível com JsonSchemaService
                var asyncCommandForSchema = RegisterAsyncIntegrationEventCommand.Create(
                    route.Destination,
                    command.DestinyKey,
                    command.Origin,
                    command.OriginKey,
                    command.Entity,
                    command.Method,
                    command.CorrelationId,
                    command.Payload,
                    command.AggregateKey,
                    command.SapQueryAction,
                    command.StatusCode,
                    command.Version,
                    command.SchemaVersion,
                    command.IsCallback,
                    command.IsCallbackError,
                    command.IsPayloadLink,
                    payloadFile,
                    payloadFileId);

                if (asyncCommandForSchema.IsFailure)
                    continue;

                var integrationEvent = jsonSchemaService.CreateFrom(
                    asyncCommandForSchema.Value,
                    route,
                    isPayloadLink: true,
                    payloadFile,
                    payloadFileId);
                
                if (integrationEvent.IsFailure)
                    continue;

                using var uniqueIdLog = LogContext.PushProperty(otelVariables.IntegrationUniqueId, integrationEvent.Value.UniqueId);
                using var controlCodeLog = LogContext.PushProperty(otelVariables.ControlCode, integrationEvent.Value.ControlCode);
                using var statusLog = LogContext.PushProperty(otelVariables.Status, integrationEvent.Value.Control.IntegrationStatus);

                // Validação
                var validationResult = await integrationEventPolicyEngine.Execute(integrationEvent.Value, cancellationToken);
                if (validationResult.IsFailure)
                    continue;

                // Salvar no banco
                await eximiaIntegrationDbContext.IntegrationEvents.AddAsync(integrationEvent.Value, cancellationToken);
                registerNewIntegrationEventTelemetry.IntegrationEventCreated(integrationEvent.Value);
                processedEvents.Add(integrationEvent.Value);

                // Publicar no Kafka
                await publisher.PublishAsync(
                    RouteIntegrationEventRequestCommand.Create(
                        integrationEvent.Value,
                        command.Origin,
                        route.Destination,
                        command.Entity,
                        route.Id));

                await publishPlataformEventService.PublishIntegrationEventReceivedAsync(integrationEvent.Value);
            }

            await eximiaIntegrationDbContext.SaveChangesAsync(cancellationToken);
            await outboxTransaction.CommitAsync(cancellationToken);
            }

            // Executar ações pós-registro para cada evento
            foreach (var integrationEvent in processedEvents)
            {
                using var eventIdLog = LogContext.PushProperty(otelVariables.IntegrationId, integrationEvent.Id);
                await integrationEventActionsEngine.Execute(integrationEvent, cancellationToken);
                registerNewIntegrationEventTelemetry.IntegrationEventPublish(integrationEvent);
            }
            
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
