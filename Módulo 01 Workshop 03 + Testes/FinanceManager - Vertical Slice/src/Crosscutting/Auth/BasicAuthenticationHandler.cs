using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace FinanceManager.Crosscutting.Auth;

public class BasicAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<BasicAuthenticationOptions> basicAuthenticationOptions) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    private readonly BasicAuthenticationOptions _basicAuthenticationOptions = basicAuthenticationOptions.Value;

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        string? authHeader = Request.Headers.Authorization;
        if (string.IsNullOrEmpty(authHeader))
        {
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
        }

        if (!authHeader.StartsWith("Basic"))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
        }

        string encodedUsernamePassword = authHeader["Basic ".Length..].Trim();
        byte[] decodedBytes = Convert.FromBase64String(encodedUsernamePassword);
        string decodedUsernamePassword = Encoding.UTF8.GetString(decodedBytes);
        string[] usernamePasswordArray = decodedUsernamePassword.Split(':');
        string username = usernamePasswordArray[0];
        string password = usernamePasswordArray[1];

        if(!_basicAuthenticationOptions.Users.TryGetValue(username, out string? userPassword) || !userPassword.Equals(password))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
        }

        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, username),
            new Claim(ClaimTypes.Name, username),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
