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
        
        // PROBLEMA: a política roda sem o token. Retry e esperas entre tentativas não podem ser cancelados.
        var result = await policy.ExecuteAsync(
            action: async context =>
            {
                using var response = await uri
                    .WithHeader("Content-Type", route.ContentType)
                    .WithHeader("x-eximia-correlation-id", integrationEvent.ControlCode)
                    .WithTimeout(TimeSpan.FromSeconds(route.TimeOut))
                    // PROBLEMA: a chamada HTTP não recebe o token: só termina pelo timeout da rota.
                    .PostStringAsync(payload);
                return integrationEvent.Control.IsSync
                    ? DispatcherResult.Success(response.StatusCode, await response.GetStringAsync())
                    : DispatcherResult.DefaultSuccess;
            },
            policyContext);
        
        return result;
    }
}
