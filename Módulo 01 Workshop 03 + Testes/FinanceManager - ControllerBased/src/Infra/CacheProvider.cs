using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace FinanceManager.Infra;

public class CacheProvider(IDistributedCache distributedCache)
{
    public async ValueTask<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> valueProvider)
    {
        var cacheContentBytes = await distributedCache.GetAsync(key);
        if (cacheContentBytes != null)
        {
            var cacheContentString = Encoding.UTF8.GetString(cacheContentBytes);
            var cacheContent = JsonSerializer.Deserialize<T>(cacheContentString);

            return cacheContent;
        }

        var value = await valueProvider();
        var valueString = JsonSerializer.Serialize(value);

        await distributedCache.SetStringAsync(key, valueString, new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromSeconds(3),
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60)
        });

        return value;
    }
}
