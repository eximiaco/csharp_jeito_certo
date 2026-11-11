using CSharpFunctionalExtensions;
using Microsoft.Extensions.Caching.Memory;

namespace Eximia.Integrations.DestinationsErrorsConfiguration.DataAccess;

public class DestinationErrorConfigurationWithMemoryCacheDecorator 
{
    private readonly DestinationErrorConfigurationDataAccess _destinationErrorConfigurationDataAccess;
    private readonly IMemoryCache _memoryCache;

    public DestinationErrorConfigurationWithMemoryCacheDecorator(
        DestinationErrorConfigurationDataAccess destinationErrorConfigurationDataAccess,
        IMemoryCache memoryCache)
    {
        _destinationErrorConfigurationDataAccess = destinationErrorConfigurationDataAccess;
        _memoryCache = memoryCache;
    }

    public async Task<Maybe<DestinationErrorConfiguration>> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _memoryCache.GetOrCreateAsync(
            key: id,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);

                return await _destinationErrorConfigurationDataAccess.GetAsync(id, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
    }
}
