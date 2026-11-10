using Eximia.Integrations.Shared.OpenTelemetry;
using Eximia.Integrations.Routing.Authentication;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.Routing.Telemetry;

public sealed class ClientCredentialsProviderTelemetry : IClientCredentialsProviderTelemetry
{
    private IOtelTelemetry? _activity = null!;
    private readonly ITelemetryFactory _telemetryFactory;
    private readonly PlatformMetrics _platformMetrics;
    private readonly OtelVariables _otelVariables;

    public ClientCredentialsProviderTelemetry(
        ITelemetryFactory telemetryFactory,
        PlatformMetrics platformMetrics,
        OtelVariables otelVariables)
    {
        _telemetryFactory = telemetryFactory;
        _platformMetrics = platformMetrics;
        _otelVariables = otelVariables;
    }

    public void StartingTokenAquisition(Route route, string serviceName)
    {
        _activity = _telemetryFactory.Create(serviceName);
        _activity.AddTag(_otelVariables.Entity, route.Entity)
            .AddTag(_otelVariables.Destiny, route.Destination)
            .AddTag(_otelVariables.RouteId, route.Id)
            .AddTag(_otelVariables.RouteAuthUri, route.Authentication.Uri);
    }
    
    public void TokenExistsInCache(JwtBearerToken token)
    {
        _activity
            .AddTag(_otelVariables.RouteAuthToken, token.access_token.AnonimateToke())
            .AddTag(_otelVariables.RouteAuthTokenExpires, token.expires_in)
            .AddEvent("Token founded in cache");
    }
    
    public void ValidToken(JwtBearerToken token)
    {
        _activity.AddEvent("Token is valid");
    }
    
    public void TokenExpired(JwtBearerToken token)
    {
        _activity.AddEvent("Token expired");
    }
    
    public void Dispose()
    {
        _activity.Dispose();
    }


    public void RequestingNewToken(Route route)
    {
        _activity.AddEvent("Requesting new token");
    }

    public void FailOnRequestingNewToken(Exception exception)
    {
        _activity.AddException("Exception on requesting new token", exception)
            .SetError("Fail on requesting token");
    }

    public void NewTokenAdquired(JwtBearerToken token)
    {
        _activity
            .AddTag(_otelVariables.RouteNewAuthToken, token.access_token.AnonimateToke())
            .AddTag(_otelVariables.RouteNewAuthTokenExpires, token.expires_in)
            .AddEvent("New token adquired");
    }

    public void SucessoOnNewToken()
    {
        _activity.SetSuccess("Token retrieved with success");
    }
}
