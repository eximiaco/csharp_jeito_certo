using Microsoft.Extensions.Caching.Memory;

namespace Eximia.Integrations.Routing.Authentication;

public abstract class TokenServiceProvider
{
    private readonly IMemoryCache _memoryCache;

    protected TokenServiceProvider(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    // PROBLEMA: sem single-flight. N mensagens simultâneas pedem N tokens ao mesmo tempo ao provedor de identidade.
    public async Task<JwtBearerToken?> GetTokenAsync(Route route, string correlationId = "")
    {
        JwtBearerToken? token = null;
        var key = $"{route.Destination}-JwtAuth";

        var cachedValue = await _memoryCache.GetOrCreateAsync(
            key,
            async cacheEntry =>
            {
                token = await RequestNewTokenAsync(route);
                // PROBLEMA: expires_in vem em SEGUNDOS, não em milissegundos. O cache dura 3,6 s em vez de 1 h.
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(token!.expires_in);
                // PROBLEMA: devolve a variável local, que só é preenchida dentro do factory. Vindo do cache, é null.
                return token;
            });

        if (!cachedValue!.Expired(TimeSpan.FromSeconds(5)))
            // PROBLEMA: o token sempre parece vencido (Expired trata o valor como instante), então pede um novo a cada chamada.
            return token;

        token = await RequestNewTokenAsync(route);
        return token;
    }

    protected abstract Task<JwtBearerToken?> RequestNewTokenAsync(Route route, string correlationId = "");
}
