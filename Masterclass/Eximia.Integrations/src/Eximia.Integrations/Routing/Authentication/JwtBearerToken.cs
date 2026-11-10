namespace Eximia.Integrations.Routing.Authentication;

public record JwtBearerToken(string access_token, string token_type, long   expires_in, string scope, string jti)
{
    public bool Expired(TimeSpan offset)
    {
        var expiration = DateTimeOffset.FromUnixTimeSeconds(expires_in);
        return DateTime.UtcNow.Add(offset) > expiration.UtcDateTime;
    }
}