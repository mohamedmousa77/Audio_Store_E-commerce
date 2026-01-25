using AudioStore.Common;
using AudioStore.Common.DTOs.Category;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Infrastructure.Cashing.Configuration;
using AudioStore.Infrastructure.Cashing.Extensions;
using AudioStore.Infrastructure.Cashing.Interfaces;
using Microsoft.Extensions.Logging;

namespace AudioStore.Infrastructure.Cashing.Decorators;

/// <summary>
/// Cached decorator for ICategoryService
/// Categories change rarely, so we can cache them for longer periods
/// </summary>
public class CachedCategoryService : ICategoryService
{
    private readonly ICategoryService _inner;
    private readonly ICacheService _cache;
    private readonly CacheConfiguration _config;
    private readonly ILogger<CachedCategoryService> _logger;

    public CachedCategoryService(
        ICategoryService inner,
        ICacheService cache,
        CacheConfiguration config,
        ILogger<CachedCategoryService> logger)
    {
        _inner = inner;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    // ============ CACHED QUERIES ============

    public async Task<Result<IEnumerable<CategoryDTO>>> GetAllAsync()
    {
        var cacheKey = CachingExtensions.GenerateCacheKey(CacheKeys.Categories, "all");

        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => _inner.GetAllAsync(),
            _config.Ttl.Categories); // 2 hours - categories change rarely
    }

    public async Task<Result<CategoryDTO>> GetByIdAsync(int id)
    {
        var cacheKey = CachingExtensions.GenerateCacheKey(CacheKeys.CategoryById, id);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => _inner.GetByIdAsync(id),
            _config.Ttl.Categories);
    }

    // ============ COMMANDS (with cache invalidation) ============

    public async Task<Result<CategoryDTO>> CreateAsync(CategoryDTO dto)
    {
        var result = await _inner.CreateAsync(dto);

        if (result.IsSuccess)
        {
            // Invalidate all category caches
            await InvalidateCategoryCaches();
            _logger.LogInformation("Category created, cache invalidated");
        }

        return result;
    }

    public async Task<Result<CategoryDTO>> UpdateAsync(CategoryDTO dto)
    {
        var result = await _inner.UpdateAsync(dto);

        if (result.IsSuccess)
        {
            // Invalidate specific category and all list caches
            await _cache.RemoveAsync(
                CachingExtensions.GenerateCacheKey(CacheKeys.CategoryById, dto.Id));

            await InvalidateCategoryCaches();
            _logger.LogInformation("Category {CategoryId} updated, cache invalidated", dto.Id);
        }

        return result;
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var result = await _inner.DeleteAsync(id);

        if (result.IsSuccess)
        {
            // Invalidate specific category and all list caches
            await _cache.RemoveAsync(
                CachingExtensions.GenerateCacheKey(CacheKeys.CategoryById, id));

            await InvalidateCategoryCaches();
            _logger.LogInformation("Category {CategoryId} deleted, cache invalidated", id);
        }

        return result;
    }

    // ============ PRIVATE HELPERS ============

    private async Task InvalidateCategoryCaches()
    {
        // Remove all category-related caches
        await _cache.RemoveByPatternAsync(CachingExtensions.GenerateCachePattern(CacheKeys.Categories));
    }
}
