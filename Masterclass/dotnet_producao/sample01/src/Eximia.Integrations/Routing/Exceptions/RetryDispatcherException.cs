namespace Eximia.Integrations.Routing.Exceptions;

public sealed class RetryDispatcherException : Exception
{
    public RetryDispatcherException(string destination, string entity, int eventId)
        : base($"Dispatcher retry required [{destination}] and [{entity} and {eventId}]")
    {
        Destination = destination;
        Entity = entity;
        EventId = eventId;
    }
    
    public string Destination { get; }
    public string Entity { get; }
    public int EventId { get; }
    
}