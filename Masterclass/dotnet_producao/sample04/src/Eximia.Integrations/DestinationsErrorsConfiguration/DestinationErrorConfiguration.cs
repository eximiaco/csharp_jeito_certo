namespace Eximia.Integrations.DestinationsErrorsConfiguration;

public class DestinationErrorConfiguration
{
    public DestinationErrorConfiguration(string id, IEnumerable<int> statusCodeAllowed, IEnumerable<DestinationErrorExceptionConfiguration> errorExceptions)
    {
        Id = id;
        StatusCodeAllowed = statusCodeAllowed;
        ErrorExceptions = errorExceptions;
    }

    public string Id { get; }
    public IEnumerable<int> StatusCodeAllowed { get; }
    public IEnumerable<DestinationErrorExceptionConfiguration> ErrorExceptions { get; }

    public bool NeedRetry(int statusCode, string response)
    {
        if (!StatusCodeAllowed.Contains(statusCode))
            return true;

        var strategies = ErrorExceptions.Where(e => e.StatusCode == statusCode);
        foreach (var strategy in strategies)
        {
            if (strategy.ResponseErrorStrategy.NeedRetry(response))
                return true;
        }

        return false;
    }
}
