using CSharpFunctionalExtensions;
using Flurl.Http;
using Polly;
using Eximia.Integrations.Shared;
using Eximia.Integrations.DestinationsErrorsConfiguration;
using Eximia.Integrations.IntegrationEvents;

namespace Eximia.Integrations.Routing.Dispatchers;

public class HttpJsonDispatcher
    : IDestinationDispatcher, IService<HttpJsonDispatcher>
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
            action: async context =>
            {
                using var response = await uri
                    .WithHeader("Content-Type", route.ContentType)
                    .WithHeader("x-eximia-correlation-id", integrationEvent.ControlCode)
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
