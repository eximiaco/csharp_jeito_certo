namespace FinanceManager.Crosscutting.Auth;

public class BasicAuthenticationOptions
{
    public Dictionary<string, string> Users { get; init; } = new Dictionary<string, string>();
}
