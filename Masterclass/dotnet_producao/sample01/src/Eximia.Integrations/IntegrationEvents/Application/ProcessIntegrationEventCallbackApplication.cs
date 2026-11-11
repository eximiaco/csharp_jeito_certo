using Serilog;
using Silverback.Messaging.Messages;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Auth;
using Eximia.Integrations.IntegrationEvents.Commands;
using Eximia.Integrations.IntegrationEvents.Services;
using static Eximia.Integrations.IntegrationEvents.Services.SendIntegrationEventService;

namespace Eximia.Integrations.IntegrationEvents.Application;

public class ProcessIntegrationEventCallbackApplication(
    IntegrationEventsDataAccess integrationEventsDataAccess,
    SendIntegrationEventService sendIntegrationEventCallbackService,
    AzureAdAuthenticationDecorator azureAdAuthenticationDecorator,
    ILogger logger)
{
    public async Task ConsumeMessage(IInboundEnvelope<ProcessIntegrationEventCallbackCommand> message, CancellationToken cancellationToken)
    {
        var command = message.Message!;

        try
        {
            var integrationEvent = await integrationEventsDataAccess.GetAsync(command.Id, cancellationToken);
            if (integrationEvent.HasNoValue)
            {
                logger.Warning("Error callback: integration event {IntegrationEventId} not found.", command.Id);
                return;
            }

            if (!integrationEvent.Value.Entity.CallbackRequired)
                return;

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
                logger.Error("Error callback: authentication failed for integration event {IntegrationEventId}. | Error: {Error}",
                    command.Id, token.Error);
                return;
            }

            var result = await sendIntegrationEventCallbackService.SendAsync(
                request,
                token.Value,
                cancellationToken);

            logger.Information("Error callback for integration event {IntegrationEventId} sent. | StatusCode: {StatusCode}",
                command.Id, result.StatusCode);
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Error callback for integration event {IntegrationEventId} failed.", command.Id);
            throw;
        }
    }
}
