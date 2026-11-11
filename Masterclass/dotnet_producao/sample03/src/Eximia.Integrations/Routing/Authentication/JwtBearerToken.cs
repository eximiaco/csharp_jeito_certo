namespace Eximia.Integrations.Routing.Authentication;

/// <param name="expires_in">Duração do token, em SEGUNDOS (padrão OAuth2).</param>
public record JwtBearerToken(string access_token, string token_type, long expires_in, string scope, string jti)
{
    // Validade ABSOLUTA, calculada no momento em que o token é recebido. Guardar só a duração e comparar
    // com o relógio depois foi a origem do cache que nunca funcionava (ver o sample 1).
    public DateTimeOffset ExpiresAt { get; } = DateTimeOffset.UtcNow.AddSeconds(expires_in);

    public bool Expired(TimeSpan margin) => DateTimeOffset.UtcNow.Add(margin) >= ExpiresAt;
}
