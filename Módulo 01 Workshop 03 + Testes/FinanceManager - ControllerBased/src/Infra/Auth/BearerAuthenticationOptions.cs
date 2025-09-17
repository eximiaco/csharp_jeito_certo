namespace FinanceManager.Infra.Auth
{
    public class BearerAuthenticationOptions
    {
        public string Secret { get; init; } = string.Empty;
        public string Audience { get; init; } = string.Empty;
        public string Issuer { get; init; } = string.Empty;
        public int ExpiresInSeconds { get; init; } = 900;
    }
}
