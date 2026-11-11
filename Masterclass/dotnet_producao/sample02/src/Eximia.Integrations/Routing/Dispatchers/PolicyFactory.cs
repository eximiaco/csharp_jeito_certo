using System.Net;
using CSharpFunctionalExtensions;
using Flurl.Http;
using Polly;
using Polly.Retry;
using Serilog;
using Eximia.Integrations.DestinationsErrorsConfiguration;
using Eximia.Integrations.Routing.Authentication;

namespace Eximia.Integrations.Routing.Dispatchers;

public static class PolicyFactory
{
    public static IAsyncPolicy<DispatcherResult> CreateClientCredentialsAuthorizePolicy(Route route, TokenServiceProvider provider, string correlationId = "") =>
        Policy<DispatcherResult>
            .Handle<FlurlHttpException>(c => c.StatusCode == (int)HttpStatusCode.Unauthorized)
            .RetryAsync(
                onRetryAsync: async (result, retry, context) =>
                {
                    var error = result.Exception.Message;
                    if (result.Exception is FlurlHttpException flurlException)
                        error = await flurlException.GetResponseStringAsync();

                    Log.Warning("Destination {Destination} returned 401, renewing the token. | Error: {Error}", route.Destination, error);
                    provider.Invalidate(route);                       // descarta o token recusado
                    await provider.GetTokenAsync(route, correlationId);
                });

    public static IAsyncPolicy<DispatcherResult> CreateRetryPolicy(int maxRetries, Maybe<DestinationErrorConfiguration> destinationErrorConfiguration) =>
        Policy<DispatcherResult>
            .Handle<FlurlHttpException>()
            .Or<FlurlHttpTimeoutException>()
            .WaitAndRetryAsync(maxRetries,
                retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
                onRetryAsync: async (result, span, retryCount, context) =>
                {
                    var error = result.Exception.Message;
                    var statusCode = 999;

                    if (result.Exception is FlurlHttpException flurlException)
                    {
                        error = await flurlException.GetResponseStringAsync();
                        statusCode = flurlException.StatusCode ?? 999;

                        if (!NeedRetry(flurlException, destinationErrorConfiguration, error))
                            throw flurlException;
                    }
                    Log.Error(result.Exception, "Retry {RetryCount}. | StatusCode: {StatusCode}. Body: {Body}", retryCount, statusCode, error);
                });

    public static IAsyncPolicy<DispatcherResult> CreateRetryPolicy() =>
        Policy<DispatcherResult>
            .Handle<FlurlHttpException>(IsTransientError)
            .Or<FlurlHttpTimeoutException>()
            .WaitAndRetryAsync(retryCount: 5,
                retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
                onRetryAsync: (result, span, retryCount, context) =>
                    Task.CompletedTask);

    public static IAsyncPolicy<DispatcherResult> CreateFallbackPolicy() =>
        Policy<DispatcherResult>
            // PROBLEMA: captura também OperationCanceledException. O shutdown vira DispatcherResult.Failure (erro falso e callback de erro).
            .Handle<Exception>()
            .FallbackAsync(
                 fallbackAction: async (result, context, token) =>
                 {
                     if (result.Exception is FlurlHttpException flurlException)
                     {
                         return DispatcherResult.Failure("Exception on call destination",
                             flurlException.StatusCode ?? 500,
                             await flurlException.GetResponseStringAsync());
                     }
                     return DispatcherResult.Failure("Destination client error");

                 },
                onFallbackAsync: async (result, context) =>
                {
                    var error = result.Exception?.Message ?? "Error";
                    var statusCode = 999;
                    
                    if (result.Exception is FlurlHttpException flurlException)
                    {
                        error = await flurlException.GetResponseStringAsync();
                        statusCode = flurlException.StatusCode ?? 999;
                    }

                    Log.Error(result.Exception, "Request failed. | StatusCode: {StatusCode}. Body: {Body}", statusCode, error);
                });

    public static AsyncRetryPolicy CreateDefaultPolicy()
        => Policy
            .Handle<FlurlHttpException>(IsTransientError)
            .WaitAndRetryAsync(5, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)));

    private static bool IsTransientError(FlurlHttpException exception)
    {
        var httpStatusCodesWorthRetrying = new[]
        {
            (int)HttpStatusCode.RequestTimeout, // 408
            (int)HttpStatusCode.BadGateway, // 502
            (int)HttpStatusCode.ServiceUnavailable, // 503
            (int)HttpStatusCode.GatewayTimeout, // 504
            (int)HttpStatusCode.TooManyRequests // 429 
        };

        return exception.StatusCode.HasValue && httpStatusCodesWorthRetrying.Contains(exception.StatusCode.Value);
    }

    private static bool NeedRetry(FlurlHttpException exception, Maybe<DestinationErrorConfiguration> destinationErrorConfiguration, string error)
    {
        if (!exception.StatusCode.HasValue)
            return false;

        var httpStatusCodesWorthRetrying = new[]
        {
            (int)HttpStatusCode.RequestTimeout, // 408
            (int)HttpStatusCode.BadGateway, // 502
            (int)HttpStatusCode.ServiceUnavailable, // 503
            (int)HttpStatusCode.GatewayTimeout, // 504
            (int)HttpStatusCode.TooManyRequests // 429 
        };

        var needRetry = httpStatusCodesWorthRetrying.Contains(exception.StatusCode.Value);
        if (needRetry)
            return needRetry;

        if (destinationErrorConfiguration.HasValue)
            needRetry = destinationErrorConfiguration.Value.NeedRetry(exception.StatusCode!.Value, error);
        return needRetry;
    }
}