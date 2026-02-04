using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace FinanceControl.IntegrationTests
{
    public class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.Email, "user@tests.com") };
            var identity = new ClaimsIdentity(claims, "TestScheme");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            var result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
    }

    public class MockSchemeProvider(IOptions<AuthenticationOptions> options) : AuthenticationSchemeProvider(options)
    {
        public override Task<AuthenticationScheme?> GetSchemeAsync(string name)
        {
            var scheme = new AuthenticationScheme(
                "TestScheme",
                "TestScheme",
                typeof(TestAuthHandler)
            );

            return Task.FromResult(scheme)!;
        }
    }

}