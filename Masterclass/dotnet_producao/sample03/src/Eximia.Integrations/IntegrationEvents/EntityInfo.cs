namespace Eximia.Integrations.IntegrationEvents;

public record EntityInfo
{
    private EntityInfo() { }

    public EntityInfo(
        string entity,
        Source origin,
        Source destiny,
        string method,
        string destinationQueue,
        string aggregateKey,
        bool callbackRequired,
        long? version)
    {
        Entity = entity;
        Origin = origin;
        Destiny = destiny;
        Method = method;
        DestinationQueue = destinationQueue;
        AggregateKey = aggregateKey;
        CallbackRequired = callbackRequired;
        Version = version;
    }

    public string Entity { get; } = string.Empty;
    public Source Origin { get; } = null!;
    public Source Destiny { get; } = null!;
    public string Method { get; } = string.Empty;
    public string DestinationQueue { get; } = string.Empty;
    public string AggregateKey { get; } = string.Empty;
    public bool CallbackRequired { get; set; }
    public long? Version { get; }
}