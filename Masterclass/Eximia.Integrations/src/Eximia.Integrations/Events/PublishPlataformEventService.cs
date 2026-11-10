using Serilog;
using Silverback.Messaging.Publishing;
using Eximia.Integrations.Shared;
using Eximia.Integrations.IntegrationEvents;

namespace Eximia.Integrations.Events;

public class PublishPlataformEventService
{
    private readonly IPublisher _publisher;
    private readonly ILogger _logger;

    public PublishPlataformEventService(IPublisher publisher, ILogger logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishIntegrationEventReceivedAsync(IntegrationEvent integrationEvent)
    {
        try
        {
            await _publisher.PublishAsync(new PlataformEvent(
                PlataformEventType.IntegrationEventReceived,
                Guid.Parse(integrationEvent.ControlCode),
                DateTime.UtcNow,
                new IntegrationEventReceivedEvent(
                    integrationEvent.Entity.Entity,
                    integrationEvent.Entity.Origin.Name,
                    integrationEvent.Entity.Destiny.Name,
                    integrationEvent.Entity.Origin.Key,
                    integrationEvent.Entity.Destiny.Key,
                    integrationEvent.Entity.Method,
                    integrationEvent.Entity.CallbackRequired))).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failure publishing platform event. | Event: {plataformEvent}. Entity: {entity}. " +
                "Origin: {origin}. OriginKey: {originKey}. Destiny: {destiny}. DestinyKey: {destinyKey}. Method: {method}. CallbackRequired: {callbackRequired}.",
                PlataformEventType.IntegrationEventReceived, integrationEvent.ControlCode, integrationEvent.Entity.Entity,
                integrationEvent.Entity.Origin.Name, integrationEvent.Entity.Origin.Key,
                integrationEvent.Entity.Destiny.Name, integrationEvent.Entity.Destiny.Key, integrationEvent.Entity.Method, integrationEvent.Entity.CallbackRequired);
            throw;
        }
    }

    public async Task PublishIntegrationEventSentAsync(string controlCode, bool isCallback, DateTime? eventDate = null)
    {
        try
        {
            await _publisher.PublishAsync(
                new PlataformEvent(PlataformEventType.IntegrationEventSent, Guid.Parse(controlCode), eventDate.HasValue ? eventDate.Value : DateTime.UtcNow,
                new IntegrationEventSentEvent(isCallback))).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failure publishing platform event. | Event: {plataformEvent}. IsCallback: {isCallback}.",
                PlataformEventType.IntegrationEventSent, controlCode, isCallback);
            throw;
        }
    }

    public async Task PublishIntegrationEventErrorAsync(IntegrationEvent integrationEvent)
    {
        try
        {
            await _publisher.PublishAsync(
                new PlataformEvent(PlataformEventType.IntegrationEventError, Guid.Parse(integrationEvent.ControlCode), DateTime.UtcNow,
                new IntegrationEventErrorEvent(integrationEvent.Retry.LastError, integrationEvent.Control.IsCallback))).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failure publishing platform event. | Event: {plataformEvent}. LastError: {lastError}. IsCallback: {isCallback}.",
                PlataformEventType.IntegrationEventError, integrationEvent.ControlCode, integrationEvent.Retry.LastError, integrationEvent.Control.IsCallback);
            throw;
        }
    }
}
