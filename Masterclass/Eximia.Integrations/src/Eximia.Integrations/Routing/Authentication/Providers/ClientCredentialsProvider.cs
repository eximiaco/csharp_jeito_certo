using Flurl.Http;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.Routing.Authentication.Providers;

public class ClientCredentialsProvider(TokenCache tokenCache)
    : TokenServiceProvider(tokenCache)
{
    protected override async Task<JwtBearerToken?> RequestNewTokenAsync(Route route, string correlationId, CancellationToken cancellationToken)
    {
        var content = new Dictionary<string, string>
        {
            ["client_id"] = route.Authentication.Client,
            ["grant_type"] = "client_credentials",
            ["client_secret"] = route.Authentication.Secret
        };

        if (!string.IsNullOrWhiteSpace(route.Authentication.Scope))
            content["scope"] = route.Authentication.Scope;

        return await route.Authentication.Uri
            .WithHeader("Content-Type", "application/x-www-form-urlencoded")
            .WithHeader("x-eximia-correlation-id", correlationId)
            .WithTimeout(TimeSpan.FromSeconds(route.TimeOut))
            .PostUrlEncodedAsync(content, cancellationToken: cancellationToken)
            .ReceiveJson<JwtBearerToken>();
    }
}
