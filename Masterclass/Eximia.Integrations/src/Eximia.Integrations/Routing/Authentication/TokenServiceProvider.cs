using Eximia.Integrations.Routing.Telemetry;

namespace Eximia.Integrations.Routing.Authentication;

public abstract class TokenServiceProvider(TokenCache tokenCache, IClientCredentialsProviderTelemetry telemetry)
{
    protected readonly IClientCredentialsProviderTelemetry _telemetry = telemetry;

    public async Task<JwtBearerToken?> GetTokenAsync(Route route, string correlationId = "", CancellationToken cancellationToken = default)
    {
        _telemetry.StartingTokenAquisition(route, "TokenServiceProvider.GetTokenAsync");

        return await tokenCache.GetAsync(
            CacheKey(route),
            async ct =>
            {
                var token = await RequestNewTokenAsync(route, correlationId, ct);
                _telemetry.NewTokenAdquired(token!);
                return token!;
            },
            cancellationToken);
    }

    /// <summary>Descarta o token em cache (por exemplo, depois de um 401) para forçar uma nova aquisição.</summary>
    public void Invalidate(Route route) => tokenCache.Invalidate(CacheKey(route));

    private static string CacheKey(Route route) => $"{route.Destination}-JwtAuth";

    protected abstract Task<JwtBearerToken?> RequestNewTokenAsync(Route route, string correlationId, CancellationToken cancellationToken);
}
