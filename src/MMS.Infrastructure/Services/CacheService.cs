using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MMS.Application.Interfaces.Services;
using System.Text.Json;

namespace MMS.Infrastructure.Services;

public class CacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CacheService>? _logger;

    public CacheService(IDistributedCache cache, ILogger<CacheService>? logger = null)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromHours(1)
            };
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), options);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to set cache for key {Key}", key);
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _cache.GetStringAsync(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to get cache for key {Key}", key);
            return default;
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _cache.RemoveAsync(key);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to remove cache for key {Key}", key);
        }
    }
}
