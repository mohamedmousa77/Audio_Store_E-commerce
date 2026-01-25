using AudioStore.Infrastructure.Cashing.Configuration;
using AudioStore.Infrastructure.Cashing.Interfaces;
using AudioStore.Infrastructure.Cashing.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AudioStore.Infrastructure.Cashing.Extensions;

/// <summary>
/// Extension methods for configuring caching services
/// </summary>
public static class CachingExtensions
{
    /// <summary>
    /// Add caching services (Redis or Memory cache based on configuration)
    /// </summary>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind cache configuration from appsettings
        var cacheConfig = configuration
            .GetSection("Caching")
            .Get<CacheConfiguration>() ?? new CacheConfiguration();

        services.AddSingleton(cacheConfig);

        if (cacheConfig.UseRedis && !string.IsNullOrEmpty(cacheConfig.RedisConnectionString))
        {
            // Configure Redis distributed cache
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = cacheConfig.RedisConnectionString;
                options.InstanceName = cacheConfig.InstanceName ?? "AudioStore:";
            });

            // Register Redis connection multiplexer
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<IConnectionMultiplexer>>();
                
                try
                {
                    var connection = ConnectionMultiplexer.Connect(cacheConfig.RedisConnectionString);
                    
                    connection.ConnectionFailed += (sender, args) =>
                    {
                        logger.LogError("Redis connection failed: {EndPoint}, {FailureType}", 
                            args.EndPoint, args.FailureType);
                    };

                    connection.ConnectionRestored += (sender, args) =>
                    {
                        logger.LogInformation("Redis connection restored: {EndPoint}", args.EndPoint);
                    };

                    logger.LogInformation("Redis connection established successfully");
                    return connection;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to connect to Redis. Falling back to Memory cache.");
                    throw;
                }
            });

            // Register Redis cache service
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            // Configure in-memory cache as fallback
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = 1024; // Limit to 1024 entries
                options.CompactionPercentage = 0.25; // Compact 25% when limit reached
            });

            // Register memory cache service
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        return services;
    }

    /// <summary>
    /// Generate cache key with prefix
    /// </summary>
    public static string GenerateCacheKey(string prefix, params object[] parts)
    {
        var key = $"{prefix}:{string.Join(":", parts)}";
        return key.ToLowerInvariant();
    }

    /// <summary>
    /// Generate cache key pattern for removal
    /// </summary>
    public static string GenerateCachePattern(string prefix)
    {
        return $"{prefix}:*";
    }
}

/// <summary>
/// Cache key prefixes for consistency
/// </summary>
public static class CacheKeys
{
    public const string Products = "products";
    public const string ProductById = "products:id";
    public const string ProductsByCategory = "products:category";
    public const string ProductsFeatured = "products:featured";
    
    public const string Categories = "categories";
    public const string CategoryById = "categories:id";
    
    public const string Dashboard = "dashboard";
    public const string DashboardSummary = "dashboard:summary";
    public const string DashboardRecentOrders = "dashboard:recent-orders";
    
    public const string Cart = "cart";
    public const string CartByUser = "cart:user";
    public const string CartBySession = "cart:session";
}
