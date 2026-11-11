using Eximia.Integrations.Shared;
using Eximia.Integrations.DestinationsErrorsConfiguration.DataAccess;
using Eximia.Integrations.IntegrationEvents;
using Eximia.Integrations.IntegrationEvents.Services;
using Eximia.Integrations.Routing.Dispatchers;

namespace Eximia.Integrations.Routing;

public class RoutingService(
    DestinationDispatcherFactory destinationDispatcherFactory,
    IDestinationErrorConfigurationDataAccess destinationErrorConfigurationDataAccess,
    PayloadService payloadService)
{
    public async Task<DispatcherResult> SendIntegrationEvent(
        IntegrationEvent integrationEvent,
        Route route,
        CancellationToken cancellationToken)
    {
        // PROBLEMA: bloqueia uma thread do pool esperando o storage. O método já é async: deveria usar await.
        var payload = payloadService.DownloadAsync(integrationEvent, route.SendPayloadAsLink, cancellationToken).Result;
        if (payload.IsFailure)
            return DispatcherResult.Failure(payload.Error);
        
        var destinationErrorConfiguration = await destinationErrorConfigurationDataAccess.GetAsync(
            integrationEvent.Entity.Destiny.Name!,
            cancellationToken);

        return await destinationDispatcherFactory
            .CreateService(route)
            .SendAsync(integrationEvent, route, destinationErrorConfiguration, payload.Value, cancellationToken);
    }
}
