using CSharpFunctionalExtensions;
using Flurl.Http;
using Polly;
using Eximia.Integrations.Shared;
using Eximia.Integrations.DestinationsErrorsConfiguration;
using Eximia.Integrations.IntegrationEvents;
using Eximia.Integrations.Routing.Authentication.Providers;

namespace Eximia.Integrations.Routing.Dispatchers;

public class HttpJsonWithJwtDispatcher(ClientCredentialsProvider clientCredentialsProvider)
    : IDestinationDispatcher, IService<HttpJsonWithJwtDispatcher>
{
    public async Task<DispatcherResult> SendAsync(
        IntegrationEvent integrationEvent,
        Route route,
        Maybe<DestinationErrorConfiguration> destinationErrorConfiguration,
        string payload,
        CancellationToken cancellationToken)
    {
        var policyContext = new Context();

        var policy = PolicyFactory
            .CreateFallbackPolicy()
            .WrapAsync(
                PolicyFactory
                    .CreateClientCredentialsAuthorizePolicy(route, clientCredentialsProvider, integrationEvent.ControlCode)
                    .WrapAsync(PolicyFactory.CreateRetryPolicy(route.MaxRetry, destinationErrorConfiguration)));

        var uri = integrationEvent.Control.IsCallbackError ? route.CallbackErrorUri : route.Uri;

        var result = await policy.ExecuteAsync(
            action: async context =>
            {
                var tokenJwt = string.Empty;
                var authToken = await clientCredentialsProvider.GetTokenAsync(route);

                if (authToken != null)
                    tokenJwt = string.IsNullOrEmpty(authToken.access_token) ? "" : $"Bearer {authToken.access_token}";

                using var response = await uri
                    .WithHeader("Content-Type", route.ContentType)
                    .WithHeader("x-eximia-correlation-id", integrationEvent.ControlCode)
                    .WithHeader("Authorization", tokenJwt)
                    .WithTimeout(TimeSpan.FromSeconds(route.TimeOut))
                    .PostStringAsync(payload);

                return integrationEvent.Control.IsSync
                    ? DispatcherResult.Success(response.StatusCode, await response.GetStringAsync())
                    : DispatcherResult.DefaultSuccess;
            },
            policyContext);
        
        return result;
    }
}
