using Flurl.Http;
using Microsoft.Extensions.Caching.Memory;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.Routing.Authentication.Providers;

public class ClientCredentialsProvider : TokenServiceProvider
{
    public ClientCredentialsProvider(IMemoryCache memoryCache) : base(memoryCache)
    { }

    protected override async Task<JwtBearerToken?> RequestNewTokenAsync(Route route, string correlationId = "")
    {
        var content = new Dictionary<string, string>
        {
            ["client_id"] = route.Authentication.Client,
            ["grant_type"] = "client_credentials",
            ["client_secret"] = route.Authentication.Secret
        };

        if (!string.IsNullOrWhiteSpace(route.Authentication.Scope))
            content["scope"] = route.Authentication.Scope;

        var foo = await route.Authentication.Uri
            .WithHeader("Content-Type", "application/x-www-form-urlencoded")
            .WithHeader("x-eximia-correlation-id", correlationId)
            .WithTimeout(TimeSpan.FromSeconds(route.TimeOut))
            .PostUrlEncodedAsync(content)
            .ReceiveJson<JwtBearerToken>();
        return foo;
    }
}
