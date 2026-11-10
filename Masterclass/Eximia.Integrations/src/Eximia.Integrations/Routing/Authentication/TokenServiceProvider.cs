using Microsoft.Extensions.Caching.Memory;

namespace Eximia.Integrations.Routing.Authentication;

public abstract class TokenServiceProvider
{
    private readonly IMemoryCache _memoryCache;

    protected TokenServiceProvider(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public async Task<JwtBearerToken?> GetTokenAsync(Route route, string correlationId = "")
    {
        JwtBearerToken? token = null;
        var key = $"{route.Destination}-JwtAuth";

        var cachedValue = await _memoryCache.GetOrCreateAsync(
            key,
            async cacheEntry =>
            {
                token = await RequestNewTokenAsync(route);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(token!.expires_in);
                return token;
            });

        if (!cachedValue!.Expired(TimeSpan.FromSeconds(5)))
            return token;

        token = await RequestNewTokenAsync(route);
        return token;
    }

    protected abstract Task<JwtBearerToken?> RequestNewTokenAsync(Route route, string correlationId = "");
}
