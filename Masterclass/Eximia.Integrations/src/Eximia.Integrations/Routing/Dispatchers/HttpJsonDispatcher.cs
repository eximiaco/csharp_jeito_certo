using CSharpFunctionalExtensions;
using Flurl.Http;
using Polly;
using Eximia.Integrations.Shared;
using Eximia.Integrations.DestinationsErrorsConfiguration;
using Eximia.Integrations.IntegrationEvents;
using Eximia.Integrations.Routing.Telemetry;

namespace Eximia.Integrations.Routing.Dispatchers;

public class HttpJsonDispatcher(IDispatcherEventTelemetry routeIntegrationEventTelemetry)
    : IDestinationDispatcher, IService<HttpJsonDispatcher>
{
    public async Task<DispatcherResult> SendAsync(
        IntegrationEvent integrationEvent,
        Route route,
        Maybe<DestinationErrorConfiguration> destinationErrorConfiguration,
        string payload,
        CancellationToken cancellationToken)
    {
        routeIntegrationEventTelemetry.IntegrationEventLoaded(nameof(HttpJsonDispatcher.SendAsync), integrationEvent, route);

        var contextWithTelemetry = new Context()
            .WithOpenTelemetry(routeIntegrationEventTelemetry)
            .WithIntegrationId(integrationEvent.Id);

        var uri = integrationEvent.Control.IsCallbackError ? route.CallbackErrorUri : route.Uri;

        var policy = PolicyFactory
            .CreateFallbackPolicy()
            .WrapAsync(PolicyFactory.CreateRetryPolicy(route.MaxRetry, destinationErrorConfiguration));
        
        var result = await policy.ExecuteAsync(
            action: async context =>
            {
                routeIntegrationEventTelemetry.MakingCallToDestiny();
                using var response = await uri
                    .WithHeader("Content-Type", route.ContentType)
                    .WithHeader("x-eximia-correlation-id", integrationEvent.ControlCode)
                    .WithTimeout(TimeSpan.FromSeconds(route.TimeOut))
                    .PostStringAsync(payload);
                routeIntegrationEventTelemetry.RequestFinished();
                return integrationEvent.Control.IsSync
                    ? DispatcherResult.Success(response.StatusCode, await response.GetStringAsync())
                    : DispatcherResult.DefaultSuccess;
            },
            contextWithTelemetry);
        
        return result;
    }
}
