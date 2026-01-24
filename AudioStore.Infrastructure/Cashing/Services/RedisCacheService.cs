using AudioStore.Infrastructure.Cashing.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace AudioStore.Infrastructure.Cashing.Services;

/// <summary>
/// Redis distributed cache implementation
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<RedisCacheService> _logger;
    private long _hits = 0;
    private long _misses = 0;

    public RedisCacheService(
        IConnectionMultiplexer redis,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _db = redis.GetDatabase();
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            
            if (value.IsNullOrEmpty)
            {
                Interlocked.Increment(ref _misses);
                _logger.LogDebug("Redis cache MISS for key: {Key}", key);
                return default;
            }

            Interlocked.Increment(ref _hits);
            _logger.LogDebug("Redis cache HIT for key: {Key}", key);
            
            return JsonSerializer.Deserialize<T>((string)value!, _jsonOptions);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error getting cache key: {Key}", key);
            return default;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error for cache key: {Key}", key);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting cache key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key, 
        T value, 
        TimeSpan? expiration = null, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var ttl = expiration ?? TimeSpan.FromMinutes(15);
            
            await _db.StringSetAsync(key, json, ttl);
            
            _logger.LogDebug("Redis cache SET for key: {Key}, expiration: {Expiration}", 
                key, ttl);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error setting cache key: {Key}", key);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON serialization error for cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error setting cache key: {Key}", key);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        // Try to get from cache first
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        _logger.LogDebug("Redis cache MISS for key: {Key}, creating new value", key);
        
        // Create new value
        var value = await factory();
        
        if (value != null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.KeyDeleteAsync(key);
            _logger.LogDebug("Redis cache REMOVE for key: {Key}", key);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error removing cache key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error removing cache key: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            if (!endpoints.Any())
            {
                _logger.LogWarning("No Redis endpoints available for pattern removal");
                return;
            }

            var server = _redis.GetServer(endpoints.First());
            
            // Use SCAN to find keys matching pattern
            var keys = server.Keys(pattern: pattern, pageSize: 1000).ToArray();
            
            if (keys.Any())
            {
                await _db.KeyDeleteAsync(keys);
                _logger.LogDebug("Redis cache REMOVE by pattern: {Pattern}, removed {Count} keys", 
                    pattern, keys.Length);
            }
            else
            {
                _logger.LogDebug("Redis cache REMOVE by pattern: {Pattern}, no keys found", pattern);
            }
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error removing cache pattern: {Pattern}", pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error removing cache pattern: {Pattern}", pattern);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.KeyExistsAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error checking key existence: {Key}", key);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error checking key existence: {Key}", key);
            return false;
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            
            foreach (var endpoint in endpoints)
            {
                var server = _redis.GetServer(endpoint);
                await server.FlushDatabaseAsync();
            }
            
            _logger.LogInformation("Redis cache cleared for all endpoints");
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis error clearing cache");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error clearing cache");
        }
    }
}
