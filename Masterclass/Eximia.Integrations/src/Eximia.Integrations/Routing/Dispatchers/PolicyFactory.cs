using System.Net;
using CSharpFunctionalExtensions;
using Flurl.Http;
using Polly;
using Polly.Retry;
using Eximia.Integrations.DestinationsErrorsConfiguration;
using Eximia.Integrations.Routing.Authentication;
using Eximia.Integrations.Routing.Telemetry;

namespace Eximia.Integrations.Routing.Dispatchers;

public static class PolicyFactory
{
    public static IAsyncPolicy<DispatcherResult> CreateClientCredentialsAuthorizePolicy(Route route, TokenServiceProvider provider, string correlationId = "") =>
        Policy<DispatcherResult>
            .Handle<FlurlHttpException>(c => c.StatusCode == (int)HttpStatusCode.Unauthorized)
            .RetryAsync(
                onRetryAsync: async (result, retry, context) =>
                {
                    var telemetry = context.GetOpenTelemetryService();
                    var error = result.Exception.Message;
                    if (result.Exception is FlurlHttpException flurlException)
                        error = await flurlException.GetResponseStringAsync();

                    telemetry!.UnAuthorizeCall();
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
                    var telemetry = context.GetOpenTelemetryService();
                    var error = result.Exception.Message;
                    var statusCode = 999;

                    if (result.Exception is FlurlHttpException flurlException)
                    {
                        error = await flurlException.GetResponseStringAsync();
                        statusCode = flurlException.StatusCode ?? 999;

                        if (!NeedRetry(flurlException, destinationErrorConfiguration, error))
                            throw flurlException;
                    }
                    telemetry!.MakeRetry(retryCount, result.Exception, error, statusCode);
                });

    public static IAsyncPolicy<DispatcherResult> CreateRetryPolicy() =>
        Policy<DispatcherResult>
            .Handle<FlurlHttpException>(IsTransientError)
            .Or<FlurlHttpTimeoutException>()
            .WaitAndRetryAsync(retryCount: 5,
                retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
                onRetryAsync: async (result, span, retryCount, context) =>
                {
                    // TODO: Log com telemetria
                    //var telemetry = context.GetOpenTelemetryService();
                    //var error = result.Exception.Message;
                    //if (result.Exception is FlurlHttpException flurlException)
                    //    error = await flurlException.GetResponseStringAsync();
                    //telemetry!.MakeRetry(result.Exception, error);
                });

    public static IAsyncPolicy<DispatcherResult> CreateFallbackPolicy() =>
        Policy<DispatcherResult>
            // Cancelamento (shutdown) NÃO é falha de entrega: sem isso o evento vira "erro" e a origem recebe
            // um callback de erro que nunca aconteceu.
            .Handle<Exception>(ex => ex is not OperationCanceledException)
            .FallbackAsync(
                 fallbackAction: async (result, context, token) =>
                 {
                     // O cliente HTTP pode embrulhar o cancelamento em outra exceção: confira o token.
                     token.ThrowIfCancellationRequested();

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
                    var telemetry = context.GetOpenTelemetryService();
                    var error = result.Exception?.Message ?? "Error";
                    var statusCode = 999;
                    
                    if (result.Exception is FlurlHttpException flurlException)
                    {
                        error = await flurlException.GetResponseStringAsync();
                        statusCode = flurlException.StatusCode ?? 999;
                    }

                    telemetry!.RequestFailed(result.Exception!, error, statusCode);
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