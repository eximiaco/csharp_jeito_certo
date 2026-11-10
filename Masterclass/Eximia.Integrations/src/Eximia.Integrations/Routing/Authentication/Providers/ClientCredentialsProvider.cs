using Flurl.Http;
using Microsoft.Extensions.Caching.Memory;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Routing.Telemetry;

namespace Eximia.Integrations.Routing.Authentication.Providers;

public class ClientCredentialsProvider : TokenServiceProvider, IService<ClientCredentialsProvider>
{
    public ClientCredentialsProvider(
        IClientCredentialsProviderTelemetry telemetry,
        IMemoryCache memoryCache) : base(memoryCache, telemetry)
    { }

    protected override async Task<JwtBearerToken?> RequestNewTokenAsync(Route route, string correlationId = "")
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
            
            var foo = await route.Authentication.Uri
                .WithHeader("Content-Type", "application/x-www-form-urlencoded")
                .WithHeader("x-eximia-correlation-id", correlationId)
                .WithTimeout(TimeSpan.FromSeconds(route.TimeOut))
                .PostUrlEncodedAsync(content)
                .ReceiveJson<JwtBearerToken>();
            return foo;
        }
        catch (Exception e)
        {
            _telemetry.FailOnRequestingNewToken(e);
            throw;
        }
    }
}
