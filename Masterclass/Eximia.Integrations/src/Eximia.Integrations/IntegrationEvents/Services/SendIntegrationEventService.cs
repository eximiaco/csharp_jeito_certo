using Eximia.Integrations.Shared;
using Eximia.Integrations.Routing.Dispatchers;
using Eximia.Integrations.Routing;
using Flurl.Http;
using Microsoft.Extensions.Configuration;
using Polly;

namespace Eximia.Integrations.IntegrationEvents.Services;

public class SendIntegrationEventService(IConfiguration configuration)
{
    private readonly string _integrationApiUrl = configuration.GetValue<string>("IntegrationApiUrl")!;

    public async Task<DispatcherResult> SendAsync(
        IntegrationEventRequest request,
        string token,
        CancellationToken cancellationToken = default)
    {
        var policy = PolicyFactory.CreateRetryPolicy();
        var result = await policy.ExecuteAndCaptureAsync(
            action: async context =>
            {
                var response = await _integrationApiUrl
                    .WithHeader("Content-Type", "application/json")
                    .WithHeader("x-eximia-correlation-id", request.metadata.correlationKey)
                    .WithOAuthBearerToken(token)
                    .PostJsonAsync(request, cancellationToken: context);

                return DispatcherResult.Success(response.StatusCode, await response.GetStringAsync());
            },
            cancellationToken: cancellationToken);

        if (result.Outcome != OutcomeType.Failure) return result.Result;
        if (result.FinalException is FlurlHttpException ex)
            return DispatcherResult.Failure(string.Empty, ex.StatusCode ?? 0, await ex.GetResponseStringAsync());
        return DispatcherResult.Failure(string.Empty, 999, result.FinalException.ToString());
    }

    public async Task<DispatcherResult> SendAsync(
        int? integrationEventId,
        string request,
        string controlCode,
        string token,
        CancellationToken cancellationToken = default)
    {
        var policy = PolicyFactory.CreateRetryPolicy();
        var result = await policy.ExecuteAsync(
            action: async context =>
            {
                var response = await _integrationApiUrl
                    .AllowAnyHttpStatus()
                    .WithHeader("Content-Type", "application/json")
                    .WithHeader("x-eximia-correlation-id", controlCode)
                    .WithHeader("x-eximia-integration-event-id", integrationEventId)
                    .WithOAuthBearerToken(token)
                    .PostStringAsync(request, cancellationToken: context);

                return !response.ResponseMessage.IsSuccessStatusCode
                    ? DispatcherResult.Failure("Failure integrating event.", response.StatusCode, await response.GetStringAsync())
                    : DispatcherResult.Success(response.StatusCode, await response.GetStringAsync());
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result;
    }

    public record IntegrationEventRequest(Metadata metadata, object data);

    public record CallbackPayload(string error);
}
