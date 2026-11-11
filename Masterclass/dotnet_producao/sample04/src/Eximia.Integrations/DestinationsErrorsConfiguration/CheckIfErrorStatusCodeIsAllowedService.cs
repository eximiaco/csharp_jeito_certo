using Eximia.Integrations.Shared;
using Eximia.Integrations.DestinationsErrorsConfiguration.DataAccess;

namespace Eximia.Integrations.DestinationsErrorsConfiguration;

public class CheckIfErrorStatusCodeIsAllowedService(
    IDestinationErrorConfigurationDataAccess destinationErrorConfigurationDataAccess)
{
    public async Task<bool> NeedRetryAsync(string destiny, string response, int statusCode, CancellationToken cancellationToken = default)
    {
        var destinationErrorConfiguration = await destinationErrorConfigurationDataAccess.GetAsync(destiny, cancellationToken).ConfigureAwait(false);
        return destinationErrorConfiguration.HasValue && destinationErrorConfiguration.Value.NeedRetry(statusCode, response);
    }
}
