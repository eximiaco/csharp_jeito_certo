namespace Eximia.Integrations.EventDispatcher.Infrastructure.Exceptions;

public sealed class NoRouteFoundException : Exception
{
    public NoRouteFoundException(string destination, string entity)
        : base($"No route found for [{destination}] and [{entity}]")
    {
        Destination = destination;
        Entity = entity;
    }

    public string Destination { get; }
    public string Entity { get; }
}