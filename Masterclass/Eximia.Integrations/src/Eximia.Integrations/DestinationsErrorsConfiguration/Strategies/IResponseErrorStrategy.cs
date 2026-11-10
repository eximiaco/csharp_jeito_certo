namespace Eximia.Integrations.DestinationsErrorsConfiguration.Strategies;

public interface IResponseErrorStrategy
{
    bool NeedRetry(string response);
}
