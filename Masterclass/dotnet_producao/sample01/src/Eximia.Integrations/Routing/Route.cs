using Eximia.Integrations.Routing.Rules;
using Eximia.Integrations.Routing.Rules.Engine;
using Eximia.Integrations.Shared;

namespace Eximia.Integrations.Routing;

public record Route(
    long Id,
    string Origin,
    string Destination,
    string Entity,
    string TopicName,
    bool CallbackRequired,
    string Uri,
    string ContentType,
    int MaxRetry,
    int TimeOut,
    string ClassName,
    EMode Mode,
    AuthenticationConfig Authentication,
    string? CallbackErrorUri,
    bool SendPayloadAsLink,
    IEnumerable<RoutingRule> Rules);


public record AuthenticationConfig
{
    public string Type { get; init; } = string.Empty;
    public string Client { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public string Uri { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;

    public static AuthenticationConfig CreateEmpty()
        => new AuthenticationConfig { Type = "none" };

    public static AuthenticationConfig CreateClientCredentials(string client, string secret, string uri)
        => new AuthenticationConfig { Type = "client_credentials", Client = client, Secret = secret, Uri = uri };
}

