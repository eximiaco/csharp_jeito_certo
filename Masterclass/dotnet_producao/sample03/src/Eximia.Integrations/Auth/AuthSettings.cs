namespace Eximia.Integrations.Auth;

public record AuthSettings
{
    public string Uri { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}