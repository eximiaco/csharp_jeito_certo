using Microsoft.Extensions.Caching.Memory;
using Eximia.Integrations.Routing.Telemetry;

namespace Eximia.Integrations.Routing.Authentication;

public abstract class TokenServiceProvider
{
    private readonly IMemoryCache _memoryCache;
    protected readonly IClientCredentialsProviderTelemetry _telemetry;

    protected TokenServiceProvider(IMemoryCache memoryCache,
        IClientCredentialsProviderTelemetry telemetry)
    {
        _memoryCache = memoryCache;
        _telemetry = telemetry;
    }

    public async Task<JwtBearerToken?> GetTokenAsync(Route route, string correlationId = "")
    {
        _telemetry.StartingTokenAquisition(route, "TokenServiceProvider.GetTokenAsync");
        JwtBearerToken? token = null;
        var key = $"{route.Destination}-JwtAuth";

        var cachedValue = await _memoryCache.GetOrCreateAsync(
            key,
            async cacheEntry =>
            {
                token = await RequestNewTokenAsync(route); 
                _telemetry.NewTokenAdquired(token!);
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(token!.expires_in);
                return token;
            });
        
        if (!cachedValue!.Expired(TimeSpan.FromSeconds(5)))
        {
            _telemetry.ValidToken(cachedValue);
            return token;
        }
        _telemetry.TokenExpired(cachedValue);
        token = await RequestNewTokenAsync(route);
        _telemetry.SucessoOnNewToken();
        return token;
    }

    protected abstract Task<JwtBearerToken?> RequestNewTokenAsync(Route route, string correlationId = "");
}