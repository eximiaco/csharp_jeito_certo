using Eximia.Integrations.Routing.Authentication;

namespace Eximia.Integrations.Routing.Telemetry;

public interface IClientCredentialsProviderTelemetry : IDisposable
{
    void RequestingNewToken(Route route);
    void FailOnRequestingNewToken(Exception exception);
    void TokenExistsInCache(JwtBearerToken token);
    void ValidToken(JwtBearerToken token);
    void TokenExpired(JwtBearerToken token);
    void NewTokenAdquired(JwtBearerToken token);
    void SucessoOnNewToken();
    void StartingTokenAquisition(Route route, string serviceName);
}