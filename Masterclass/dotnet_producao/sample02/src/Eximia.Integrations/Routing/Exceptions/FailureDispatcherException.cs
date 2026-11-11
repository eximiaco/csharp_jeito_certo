namespace Eximia.Integrations.Routing.Exceptions;

public sealed class FailureDispatcherException : Exception
{
    public FailureDispatcherException(long eventId, string message) : base(message)
    {
        Destination = string.Empty;
        Entity = string.Empty;
        EventId = eventId;
        Detail = message;
    }

    public FailureDispatcherException(string destination, string entity, long eventId, string detail, Exception? inner = null)
        : base($"Dispatcher failure for destiny [{destination}] and entity [{entity} and event {eventId} with details: {detail}]", inner)
    {
        Destination = destination;
        Entity = entity;
        EventId = eventId;
        Detail = detail;
    }

    public FailureDispatcherException(string destination, string entity, long eventId, string detail, int? statusCode)
        : base($"Dispatcher failure for destiny [{destination}] and entity [{entity} and event {eventId} with status code: {statusCode} and details: {detail}]")
    {
        Destination = destination;
        Entity = entity;
        EventId = eventId;
        Detail = detail;
        StatusCode = statusCode;
    }

    public string Destination { get; }
    public string Entity { get; }
    public long EventId { get; }
    public string Detail { get; }
    public int? StatusCode { get; }
}