using Serilog;
using Silverback.Messaging.Publishing;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Shared.Extensions;
using Eximia.Integrations.DomainServices;
using Eximia.Integrations.Events;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.IntegrationEvents.Services;
using Eximia.Integrations.Outbox;
using Eximia.Integrations.Outbox.EntityFramework;
using Eximia.Integrations.Routing;
using Eximia.Integrations.Routing.Rules.Engine;

namespace Eximia.Integrations.IntegrationEvents.Application.Register;

public class RegisterMultipleRoutesIntegrationEventApplication(
    JsonSchemaService jsonSchemaService,
    RoutingDataAccess routingDataAccess,
    IntegrationEventActionsEngine integrationEventActionsEngine,
    IPublisher publisher,
    EximiaIntegrationDbContext eximiaIntegrationDbContext,
    PublishPlataformEventService publishPlataformEventService,
    RoutingRuleEngine routingRuleEngine,
    PayloadService uploadIntegrationEventPayloadService,
    ILogger logger)
{
    public async Task<ResultAggregate> HandleV3(
        RegisterMultipleRoutesIntegrationEventCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Buscar TODAS as rotas para origem + entidade
            var allRoutes = await routingDataAccess.GetRoutesByOriginAndEntityAsync(
                command.Origin,
                command.Entity,
                cancellationToken);

            var allRoutesList = allRoutes.ToList();
            if (allRoutesList.Count == 0)
            {
                logger.Error("No route found for origin {Origin} and entity {Entity}.", command.Origin, command.Entity);
                return ResultAggregate.Failure("Destiny", "No route for destiny and entity");
            }

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
            var payloadFileId = Guid.NewGuid();

            var uploadResult = await uploadIntegrationEventPayloadService.UploadAsync(
                payload,
                payloadFileId,
                cancellationToken);

            var payloadFile = uploadResult.FileName;

            // 3. Processar cada rota aplicável
            var processedEvents = new List<IntegrationEvent>();

            // Integration events + mensagens do outbox do Silverback são confirmados na mesma transação.
            await using var outboxTransaction = await OutboxTransaction.BeginAsync(eximiaIntegrationDbContext, publisher, cancellationToken);
            foreach (var route in applicableRoutesList)
            {
                var integrationEvent = jsonSchemaService.CreateFrom(
                    command,
                    route,
                    payloadFile,
                    payloadFileId);

                if (integrationEvent.IsFailure)
                    continue;

                // Salvar no banco
                await eximiaIntegrationDbContext.IntegrationEvents.AddAsync(integrationEvent.Value, cancellationToken);
                processedEvents.Add(integrationEvent.Value);

                // Publicar no Kafka
                await publisher.PublishAsync(
                    RouteIntegrationEventRequestCommand.Create(
                        integrationEvent.Value,
                        command.Origin,
                        route.Destination,
                        command.Entity,
                        route.Id),
                    cancellationToken);

                // Evento da plataforma: roda em paralelo para não atrasar a resposta à origem.
                // PROBLEMA: task solta. Não é aguardada nem observada: a exceção some, o trabalho se perde se o app parar,
                // e o IPublisher (scoped) pode já estar descartado quando ela rodar.
                _ = Task.Run(() =>
                    publishPlataformEventService.PublishIntegrationEventReceivedAsync(integrationEvent.Value));
            }

            await eximiaIntegrationDbContext.SaveChangesAsync(cancellationToken);
            await outboxTransaction.CommitAsync(cancellationToken);

            // Executar ações pós-registro para cada evento
            // PROBLEMA: lambda async num Parallel.ForEach vira async void. Não espera o trabalho, uma exceção derruba o processo
            // e o DbContext é usado por várias tasks ao mesmo tempo.
            Parallel.ForEach(processedEvents,
                async integrationEvent =>
                {
                    await integrationEventActionsEngine.Execute(integrationEvent, cancellationToken);
                });

            return ResultAggregate.Success();
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Fatal error registering integration event for origin {Origin} and entity {Entity}.",
                command.Origin, command.Entity);
            throw;
        }
    }
}