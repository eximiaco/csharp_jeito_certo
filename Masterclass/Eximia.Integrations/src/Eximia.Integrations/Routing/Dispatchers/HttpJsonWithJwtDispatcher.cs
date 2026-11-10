using CSharpFunctionalExtensions;
using Flurl.Http;
using Polly;
using Eximia.Integrations.Shared;
using Eximia.Integrations.DestinationsErrorsConfiguration;
using Eximia.Integrations.IntegrationEvents;
using Eximia.Integrations.Routing.Authentication.Providers;
using Eximia.Integrations.Routing.Telemetry;

namespace Eximia.Integrations.Routing.Dispatchers;

public class HttpJsonWithJwtDispatcher(
    IDispatcherEventTelemetry routeIntegrationEventTelemetry,
    ClientCredentialsProvider clientCredentialsProvider)
    : IDestinationDispatcher
{
    public async Task<DispatcherResult> SendAsync(
        IntegrationEvent integrationEvent,
        Route route,
        Maybe<DestinationErrorConfiguration> destinationErrorConfiguration,
        string payload,
        CancellationToken cancellationToken)
    {
        routeIntegrationEventTelemetry.IntegrationEventLoaded(nameof(HttpJsonWithJwtDispatcher.SendAsync), integrationEvent, route);
        var contextWithTelemetry = new Context()
            .WithOpenTelemetry(routeIntegrationEventTelemetry)
            .WithIntegrationId(integrationEvent.Id);

        var policy = PolicyFactory
            .CreateFallbackPolicy()
            .WrapAsync(
                PolicyFactory
                    .CreateClientCredentialsAuthorizePolicy(route, clientCredentialsProvider, integrationEvent.ControlCode)
                    .WrapAsync(PolicyFactory.CreateRetryPolicy(route.MaxRetry, destinationErrorConfiguration)));

        var uri = integrationEvent.Control.IsCallbackError ? route.CallbackErrorUri : route.Uri;

        var result = await policy.ExecuteAsync(
            // O token entra na POLÍTICA: é ela que o entrega a cada tentativa (token) e o usa nas esperas entre elas.
            action: async (context, token) =>
            {
                routeIntegrationEventTelemetry.MakingCallToDestiny();
                var tokenJwt = string.Empty;
                var authToken = await clientCredentialsProvider.GetTokenAsync(route, integrationEvent.ControlCode, token);

                if (authToken != null)
                    tokenJwt = string.IsNullOrEmpty(authToken.access_token) ? "" : $"Bearer {authToken.access_token}";

                using var response = await uri
                    .WithHeader("Content-Type", route.ContentType)
                    .WithHeader("x-eximia-correlation-id", integrationEvent.ControlCode)
                    .WithHeader("Authorization", tokenJwt)
                    .WithTimeout(TimeSpan.FromSeconds(route.TimeOut))
                    .PostStringAsync(payload, cancellationToken: token);

                routeIntegrationEventTelemetry.RequestFinished();
                return integrationEvent.Control.IsSync
                    ? DispatcherResult.Success(response.StatusCode, await response.GetStringAsync())
                    : DispatcherResult.DefaultSuccess;
            },
            contextWithTelemetry,
            cancellationToken);
        
        return result;
    }
}
