namespace Eximia.Integrations.Routing.Authentication;

public abstract class TokenServiceProvider(TokenCache tokenCache)
{
    public async Task<JwtBearerToken?> GetTokenAsync(Route route, string correlationId = "", CancellationToken cancellationToken = default)
    {
        return await tokenCache.GetAsync(
            CacheKey(route),
            async ct => (await RequestNewTokenAsync(route, correlationId, ct))!,
            cancellationToken);
    }

    /// <summary>Descarta o token em cache (por exemplo, depois de um 401) para forçar uma nova aquisição.</summary>
    public void Invalidate(Route route) => tokenCache.Invalidate(CacheKey(route));

    private static string CacheKey(Route route) => $"{route.Destination}-JwtAuth";

    protected abstract Task<JwtBearerToken?> RequestNewTokenAsync(Route route, string correlationId, CancellationToken cancellationToken);
}
