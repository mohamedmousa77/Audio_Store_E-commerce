using AudioStore.Application.DTOs.Admin.Dashboard;
using AudioStore.Application.Services.Interfaces;
using AudioStore.Common.Result;
using AudioStore.Infrastructure.Cashing.Configuration;
using AudioStore.Infrastructure.Cashing.Extensions;
using AudioStore.Infrastructure.Cashing.Interfaces;
using Microsoft.Extensions.Logging;

namespace AudioStore.Infrastructure.Cashing.Decorators;

/// <summary>
/// Cached decorator for IDashboardService
/// Dashboard stats are cached for short periods (5 minutes) for near real-time data
/// </summary>
public class CachedDashboardService : IDashboardService
{
    private readonly IDashboardService _inner;
    private readonly ICacheService _cache;
    private readonly CacheConfiguration _config;
    private readonly ILogger<CachedDashboardService> _logger;

    public CachedDashboardService(
        IDashboardService inner,
        ICacheService cache,
        CacheConfiguration config,
        ILogger<CachedDashboardService> logger)
    {
        _inner = inner;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    public async Task<Result<DashboardStatsDTO>> GetDashboardStatsAsync()
    {
        var cacheKey = CachingExtensions.GenerateCacheKey(CacheKeys.DashboardSummary);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => _inner.GetDashboardStatsAsync(),
            _config.Ttl.Dashboard); // 5 minutes - near real-time stats
    }

    /// <summary>
    /// Invalidate dashboard cache (called when orders/products change)
    /// </summary>
    public async Task InvalidateDashboardCacheAsync()
    {
        await _cache.RemoveByPatternAsync(CachingExtensions.GenerateCachePattern(CacheKeys.Dashboard));
        _logger.LogInformation("Dashboard cache invalidated");
    }
}
