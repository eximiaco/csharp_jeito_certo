using Flurl.Http;
using Polly;
using Polly.Retry;
using System.Net;

namespace GymErp.Common;

public sealed class HttpRetryPolicy
{
    public static AsyncRetryPolicy AsyncRetryPolicy
        => Policy
            .Handle<FlurlHttpException>(IsTransientError)
            .WaitAndRetryAsync(5, retry => TimeSpan.FromSeconds(Math.Pow(2, retry)));

    private static bool IsTransientError(FlurlHttpException exception)
    {
        var httpStatusCodesWorthRetrying = new List<int>(capacity: 4)
            {
                (int)HttpStatusCode.RequestTimeout, // 408
                (int)HttpStatusCode.BadGateway, // 502
                (int)HttpStatusCode.ServiceUnavailable, // 503
                (int)HttpStatusCode.GatewayTimeout // 504
            };
        return exception.StatusCode.HasValue && httpStatusCodesWorthRetrying.Contains(exception.StatusCode.Value);
    }
}
