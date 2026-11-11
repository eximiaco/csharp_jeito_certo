using CSharpFunctionalExtensions;
using Microsoft.Extensions.Caching.Memory;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.Auth;

public class AzureAdAuthenticationDecorator(
    AzureAdAuthenticationService azureAdAuthenticationService,
    IMemoryCache memoryCache)
{
    public async Task<Result<string>> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        var key = "Eximia-AzureAD";
        var cachedToken = await memoryCache.GetOrCreateAsync(
            key,
            async cacheEntry =>
            {
                var token = await azureAdAuthenticationService.AuthenticateAsync(cancellationToken).ConfigureAwait(false);
                if (token.IsSuccess)
                    cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(token.Value.ExpiresIn - 5);
                return token;
            }).ConfigureAwait(false);

        if (cachedToken.IsFailure)
        {
            memoryCache.Remove(key);
            return Result.Failure<string>(cachedToken.Error);
        }
        return cachedToken.Value.AccessToken;
    }
}
