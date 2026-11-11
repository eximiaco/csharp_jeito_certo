namespace Eximia.Integrations.DestinationsErrorsConfiguration.Strategies;

public class StringResponseErrorStrategy : IResponseErrorStrategy
{
    public StringResponseErrorStrategy(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public bool NeedRetry(string response)
        => Value.Equals(response, StringComparison.OrdinalIgnoreCase);
}
