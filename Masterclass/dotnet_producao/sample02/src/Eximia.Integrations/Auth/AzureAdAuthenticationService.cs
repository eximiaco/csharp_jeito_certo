using CSharpFunctionalExtensions;
using Flurl.Http;
using Microsoft.Extensions.Options;
using Polly;
using Eximia.Integrations.Shared;
using Eximia.Integrations.Routing.Dispatchers;

namespace Eximia.Integrations.Auth;

public class AzureAdAuthenticationService(IOptions<AuthSettings> authSettings)
{
    public async Task<Result<AuthenticationResult>> AuthenticateAsync(CancellationToken cancellationToken)
    {
        var response = await PolicyFactory.CreateDefaultPolicy().ExecuteAndCaptureAsync(
            action: async context =>
            {
                return await authSettings.Value.Uri
                    .WithBasicAuth(authSettings.Value.ClientId, authSettings.Value.ClientSecret)
                    .PostUrlEncodedAsync(new
                    {
                        scope = authSettings.Value.Scope,
                        grant_type = "client_credentials"
                    })
                    .ConfigureAwait(false);
            },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (response.Outcome == OutcomeType.Failure)
        {
            if (response.FinalException is FlurlHttpException exception)
                return Result.Failure<AuthenticationResult>(await exception.GetResponseStringAsync().ConfigureAwait(false));
            return Result.Failure<AuthenticationResult>(response.FinalException.ToString());
        }

        if (!response.Result.ResponseMessage.IsSuccessStatusCode)
            return Result.Failure<AuthenticationResult>(await response.Result.GetStringAsync().ConfigureAwait(false));

        return await response.Result.GetJsonAsync<AuthenticationResult>().ConfigureAwait(false);
    }
}
