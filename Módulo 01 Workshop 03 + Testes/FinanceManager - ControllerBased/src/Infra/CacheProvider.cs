using Microsoft.Extensions.Caching.Distributed;
using System.Text;
using System.Text.Json;

namespace FinanceManager.Infra;

public class CacheProvider(IDistributedCache distributedCache)
{
    public async ValueTask<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> valueProvider,
        TimeSpan? slidingExpiration = null,
        TimeSpan? absoluteExpiration = null)
    {
        var cacheContentBytes = await distributedCache.GetAsync(key);
        if (cacheContentBytes != null)
        {
            var cacheContentString = Encoding.UTF8.GetString(cacheContentBytes);
            var cacheContent = JsonSerializer.Deserialize<T>(cacheContentString);

            return cacheContent;
        }

        var value = await valueProvider();
        
        // Não cacheia valores null para evitar cachear resultados de "não encontrado"
        if (value == null)
        {
            return value;
        }

        var valueString = JsonSerializer.Serialize(value);

        var cacheOptions = new DistributedCacheEntryOptions();
        
        if (slidingExpiration.HasValue)
        {
            cacheOptions.SlidingExpiration = slidingExpiration.Value;
        }
        
        if (absoluteExpiration.HasValue)
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
        }
        else if (!slidingExpiration.HasValue)
        {
            // Default: 60 segundos absoluto se nenhum TTL for especificado
            cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
        }

        await distributedCache.SetStringAsync(key, valueString, cacheOptions);

        return value;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var cacheContentBytes = await distributedCache.GetAsync(key);
        if (cacheContentBytes == null)
        {
            return default;
        }

        var cacheContentString = Encoding.UTF8.GetString(cacheContentBytes);
        return JsonSerializer.Deserialize<T>(cacheContentString);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? slidingExpiration = null,
        TimeSpan? absoluteExpiration = null)
    {
        var valueString = JsonSerializer.Serialize(value);
        
        var cacheOptions = new DistributedCacheEntryOptions();
        
        if (slidingExpiration.HasValue)
        {
            cacheOptions.SlidingExpiration = slidingExpiration.Value;
        }
        
        if (absoluteExpiration.HasValue)
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = absoluteExpiration.Value;
        }
        else if (!slidingExpiration.HasValue)
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
        }

        await distributedCache.SetStringAsync(key, valueString, cacheOptions);
    }

    public async Task RemoveAsync(string key)
    {
        await distributedCache.RemoveAsync(key);
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        // Nota: Redis não suporta nativamente remoção por padrão via IDistributedCache
        // Em produção, considere usar StackExchange.Redis diretamente para operações avançadas
        // Por enquanto, este método serve como placeholder para documentação
        // Para implementação real, seria necessário injetar IConnectionMultiplexer do StackExchange.Redis
        await Task.CompletedTask;
    }
}
