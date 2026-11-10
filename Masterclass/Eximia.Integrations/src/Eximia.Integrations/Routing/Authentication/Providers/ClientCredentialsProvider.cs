using Flurl.Http;
using Microsoft.Extensions.Caching.Memory;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Routing.Telemetry;

namespace Eximia.Integrations.Routing.Authentication.Providers;

public class ClientCredentialsProvider(TokenCache tokenCache, IClientCredentialsProviderTelemetry telemetry)
    : TokenServiceProvider(tokenCache, telemetry), IService<ClientCredentialsProvider>
{
    protected override async Task<JwtBearerToken?> RequestNewTokenAsync(Route route, string correlationId, CancellationToken cancellationToken)
    {
        _telemetry.RequestingNewToken(route);
        try
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
        catch (Exception e)
        {
            _telemetry.FailOnRequestingNewToken(e);
            throw;
        }
    }
}
