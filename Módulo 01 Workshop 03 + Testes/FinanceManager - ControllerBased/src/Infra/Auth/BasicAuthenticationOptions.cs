namespace FinanceManager.Infra.Auth;

public class BasicAuthenticationOptions
{
    public Dictionary<string, string> Users { get; init; } = new Dictionary<string, string>();
}
