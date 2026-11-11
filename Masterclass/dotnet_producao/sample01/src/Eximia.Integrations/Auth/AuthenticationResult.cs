using System.Text.Json.Serialization;

namespace Eximia.Integrations.Auth;

public record AuthenticationResult(
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("access_token")] string AccessToken);
