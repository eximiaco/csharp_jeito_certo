namespace Eximia.Integrations.Routing;

public record RouteAuthenticationSettings
{
    public string Destiny { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string Uri { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}
