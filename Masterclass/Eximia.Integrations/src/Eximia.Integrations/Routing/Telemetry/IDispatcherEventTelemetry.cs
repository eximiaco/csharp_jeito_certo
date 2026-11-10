using Eximia.Integrations.IntegrationEvents;

namespace Eximia.Integrations.Routing.Telemetry;

public interface IDispatcherEventTelemetry : IDisposable
{
    void RequestFinished();
    void IntegrationEventLoaded(string serviceName, IntegrationEvent integrationEvent, Route route);
    void MakingCallToDestiny();
    void MakeRetry(int retryCount, Exception resultException, string body, int statusCode);
    void RequestFailed(Exception resultException, string error, int statusCode);
    void UnAuthorizeCall();
}