using CSharpFunctionalExtensions;
using Flurl.Http;
using Polly;
using Eximia.Integrations.Shared;
using Eximia.Integrations.DestinationsErrorsConfiguration;
using Eximia.Integrations.IntegrationEvents;

namespace Eximia.Integrations.Routing.Dispatchers;

public class HttpJsonDispatcher
    : IDestinationDispatcher
{
    public async Task<DispatcherResult> SendAsync(
        IntegrationEvent integrationEvent,
        Route route,
        Maybe<DestinationErrorConfiguration> destinationErrorConfiguration,
        string payload,
        CancellationToken cancellationToken)
    {
        var policyContext = new Context();

        var uri = integrationEvent.Control.IsCallbackError ? route.CallbackErrorUri : route.Uri;

        var policy = PolicyFactory
            .CreateFallbackPolicy()
            .WrapAsync(PolicyFactory.CreateRetryPolicy(route.MaxRetry, destinationErrorConfiguration));
        
        var result = await policy.ExecuteAsync(
            // O token entra na POLÍTICA: é ela que o entrega a cada tentativa (token) e o usa nas esperas entre elas.
            action: async (context, token) =>
            {
                using var response = await uri
                    .WithHeader("Content-Type", route.ContentType)
                    .WithHeader("x-eximia-correlation-id", integrationEvent.ControlCode)
                    .WithTimeout(TimeSpan.FromSeconds(route.TimeOut))
                    .PostStringAsync(payload, cancellationToken: token);
                return integrationEvent.Control.IsSync
                    ? DispatcherResult.Success(response.StatusCode, await response.GetStringAsync())
                    : DispatcherResult.DefaultSuccess;
            },
            policyContext,
            cancellationToken);
        
        return result;
    }
}
