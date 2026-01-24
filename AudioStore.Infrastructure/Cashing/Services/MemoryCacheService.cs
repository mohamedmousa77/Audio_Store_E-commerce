using AudioStore.Infrastructure.Cashing.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AudioStore.Infrastructure.Cashing.Services;

/// <summary>
/// In-memory cache implementation using IMemoryCache
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryCacheService> _logger;
    private long _hits = 0;
    private long _misses = 0;

    public MemoryCacheService(
        IMemoryCache cache,
        ILogger<MemoryCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                Interlocked.Increment(ref _hits);
                _logger.LogDebug("Cache HIT for key: {Key}", key);
                return Task.FromResult(value);
            }

            Interlocked.Increment(ref _misses);
            _logger.LogDebug("Cache MISS for key: {Key}", key);
            return Task.FromResult<T?>(default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cache key: {Key}", key);
            return Task.FromResult<T?>(default);
        }
    }

    public Task SetAsync<T>(
        string key, 
        T value, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var options = new MemoryCacheEntryOptions();
            
            if (expiration.HasValue)
            {
                options.AbsoluteExpirationRelativeToNow = expiration.Value;
            }
            else
            {
                // Default 15 minutes if not specified
                options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            }

            // Set sliding expiration to 1/3 of absolute expiration
            options.SlidingExpiration = TimeSpan.FromMinutes(
                (expiration ?? TimeSpan.FromMinutes(15)).TotalMinutes / 3);

            _cache.Set(key, value, options);
            _logger.LogDebug("Cache SET for key: {Key}, expiration: {Expiration}", 
                key, expiration ?? TimeSpan.FromMinutes(15));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        _logger.LogDebug("Cache MISS for key: {Key}, creating new value", key);
        var value = await factory();
        
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            _cache.Remove(key);
            _logger.LogDebug("Cache REMOVE for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache key: {Key}", key);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        // Memory cache doesn't natively support pattern-based removal
        // This is a limitation - for pattern removal, use Redis
        _logger.LogWarning(
            "Pattern-based cache removal not supported in MemoryCache. Pattern: {Pattern}", 
            pattern);
        
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var exists = _cache.TryGetValue(key, out _);
        return Task.FromResult(exists);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Compact the cache to remove all entries
            if (_cache is MemoryCache memCache)
            {
                memCache.Compact(1.0); // Remove 100% of entries
                _logger.LogInformation("Memory cache cleared");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cache");
        }

        return Task.CompletedTask;
    }
}
