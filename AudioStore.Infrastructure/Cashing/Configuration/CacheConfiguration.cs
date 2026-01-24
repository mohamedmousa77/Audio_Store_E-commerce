namespace AudioStore.Infrastructure.Cashing.Configuration;

/// <summary>
/// Configuration settings for caching
/// </summary>
public class CacheConfiguration
{
    /// <summary>
    /// Whether to use Redis cache (true) or Memory cache (false)
    /// </summary>
    public bool UseRedis { get; set; } = false;

    /// <summary>
    /// Redis connection string
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Instance name for Redis cache (optional)
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// TTL settings for different cache types
    /// </summary>
    public CacheTtlSettings Ttl { get; set; } = new();

    /// <summary>
    /// Enable cache statistics tracking
    /// </summary>
    public bool EnableStatistics { get; set; } = true;
}

/// <summary>
/// Time-to-live settings for different cache categories
/// </summary>
public class CacheTtlSettings
{
    /// <summary>
    /// Default TTL for all cache entries (15 minutes)
    /// </summary>
    public TimeSpan Default { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// TTL for product data (1 hour)
    /// </summary>
    public TimeSpan Products { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// TTL for category data (2 hours - categories change rarely)
    /// </summary>
    public TimeSpan Categories { get; set; } = TimeSpan.FromHours(2);

    /// <summary>
    /// TTL for dashboard statistics (5 minutes - near real-time)
    /// </summary>
    public TimeSpan Dashboard { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// TTL for user cart (30 minutes)
    /// </summary>
    public TimeSpan Cart { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// TTL for user session data (1 hour)
    /// </summary>
    public TimeSpan Session { get; set; } = TimeSpan.FromHours(1);
}
