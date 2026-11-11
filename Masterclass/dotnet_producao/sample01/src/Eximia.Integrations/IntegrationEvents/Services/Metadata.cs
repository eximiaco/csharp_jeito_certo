using Eximia.Integrations.Shared.Extensions;

namespace Eximia.Integrations.IntegrationEvents.Services;

public record Metadata(Source origin, Source destiny, Entity entity, string correlationKey, int statusCode, bool isCallback, bool isCallbackError)
{
    public static Metadata CreateForErrorCallback(Source origin, Source destiny, Entity entity, string correlationKey, int statusCode)
        => new Metadata(origin, destiny, entity, correlationKey, statusCode, isCallback: true, isCallbackError: true);

    public static Metadata Create(Source origin, Source destiny, Entity entity, string correlationKey)
        => new Metadata(origin, destiny, entity, correlationKey, statusCode: 200, isCallback: false, isCallbackError: false);
}

public record Entity(string name, long version, string? action, string? aggregateKey)
{
    public static Entity Create(string name, string action, string aggregateKey)
        => new Entity(name, version: DateTime.Now.ConvertToUnixTime(), action, aggregateKey);
}
