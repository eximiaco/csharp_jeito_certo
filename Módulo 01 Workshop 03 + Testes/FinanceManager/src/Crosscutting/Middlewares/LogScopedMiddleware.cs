using Microsoft.Extensions.Primitives;

namespace FinanceManager.Crosscutting.Middlewares
{
    public class LogScopedMiddleware(RequestDelegate next, ILogger<LogScopedMiddleware> logger)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var requestHeaders = context.Request.Headers;
            var correlationId = requestHeaders.TryGetValue("X-Correlation-ID", out StringValues value)
                ? value.ToString()
                : Guid.NewGuid().ToString();

            using (logger.BeginScope("{CorrelationId}", correlationId))
            {
                await next(context);
            }
        }
    }
}
