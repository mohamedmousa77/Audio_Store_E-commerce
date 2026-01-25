using AudioStore.Common;
using AudioStore.Common.DTOs.Products;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Infrastructure.Cashing.Configuration;
using AudioStore.Infrastructure.Cashing.Extensions;
using AudioStore.Infrastructure.Cashing.Interfaces;
using Microsoft.Extensions.Logging;

namespace AudioStore.Infrastructure.Cashing.Decorators;

/// <summary>
/// Cached decorator for IProductService using decorator pattern
/// </summary>
public class CachedProductService : IProductService
{
    private readonly IProductService _inner;
    private readonly ICacheService _cache;
    private readonly CacheConfiguration _config;
    private readonly ILogger<CachedProductService> _logger;

    public CachedProductService(
        IProductService inner,
        ICacheService cache,
        CacheConfiguration config,
        ILogger<CachedProductService> logger)
    {
        _inner = inner;
        _cache = cache;
        _config = config;
        _logger = logger;
    }

    // ============ CACHED QUERIES ============

    public async Task<Result<ProductDTO>> GetByIdAsync(int id)
    {
        var cacheKey = CachingExtensions.GenerateCacheKey(CacheKeys.ProductById, id);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => _inner.GetByIdAsync(id),
            _config.Ttl.Products);
    }

    public async Task<Result<PaginatedResult<ProductDTO>>> GetAllAsync(ProductFilterDTO filter)
    {
        // Don't cache paginated results with filters - too many combinations
        // Only cache if no filters applied (default view)
        if (IsDefaultFilter(filter))
        {
            var cacheKey = CachingExtensions.GenerateCacheKey(
                CacheKeys.Products,
                "all",
                filter.PageNumber,
                filter.PageSize);

            return await _cache.GetOrCreateAsync(
                cacheKey,
                () => _inner.GetAllAsync(filter),
                _config.Ttl.Products);
        }

        return await _inner.GetAllAsync(filter);
    }

    public async Task<Result<IEnumerable<ProductDTO>>> GetFeaturedAsync(int count = 10)
    {
        var cacheKey = CachingExtensions.GenerateCacheKey(CacheKeys.ProductsFeatured, count);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => _inner.GetFeaturedAsync(count),
            _config.Ttl.Products);
    }

    public async Task<Result<IEnumerable<ProductDTO>>> GetByCategoryAsync(int categoryId)
    {
        var cacheKey = CachingExtensions.GenerateCacheKey(CacheKeys.ProductsByCategory, categoryId);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => _inner.GetByCategoryAsync(categoryId),
            _config.Ttl.Products);
    }

    public async Task<Result<IEnumerable<ProductDTO>>> SearchAsync(string searchTerm)
    {
        // Don't cache search results - too many combinations and usually one-time queries
        return await _inner.SearchAsync(searchTerm);
    }

    public async Task<Result<IEnumerable<string>>> GetBrandsAsync()
    {
        var cacheKey = CachingExtensions.GenerateCacheKey(CacheKeys.Products, "brands");

        return await _cache.GetOrCreateAsync(
            cacheKey,
            () => _inner.GetBrandsAsync(),
            _config.Ttl.Products);
    }

    // ============ COMMANDS (with cache invalidation) ============

    public async Task<Result<ProductDTO>> CreateAsync(CreateProductDTO dto)
    {
        var result = await _inner.CreateAsync(dto);

        if (result.IsSuccess)
        {
            // Invalidate all product caches
            await InvalidateProductCaches();
            _logger.LogInformation("Product created, cache invalidated");
        }

        return result;
    }

    public async Task<Result<ProductDTO>> UpdateAsync(UpdateProductDTO dto)
    {
        var result = await _inner.UpdateAsync(dto);

        if (result.IsSuccess)
        {
            // Invalidate specific product and all list caches
            await _cache.RemoveAsync(
                CachingExtensions.GenerateCacheKey(CacheKeys.ProductById, dto.Id));

            await InvalidateProductCaches();
            _logger.LogInformation("Product {ProductId} updated, cache invalidated", dto.Id);
        }

        return result;
    }

    public async Task<Result> DeleteAsync(int id)
    {
        var result = await _inner.DeleteAsync(id);

        if (result.IsSuccess)
        {
            // Invalidate specific product and all list caches
            await _cache.RemoveAsync(
                CachingExtensions.GenerateCacheKey(CacheKeys.ProductById, id));

            await InvalidateProductCaches();
            _logger.LogInformation("Product {ProductId} deleted, cache invalidated", id);
        }

        return result;
    }

    public async Task<Result> UpdateStockAsync(int id, int quantity)
    {
        var result = await _inner.UpdateStockAsync(id, quantity);

        if (result.IsSuccess)
        {
            // Invalidate specific product cache
            await _cache.RemoveAsync(
                CachingExtensions.GenerateCacheKey(CacheKeys.ProductById, id));

            _logger.LogInformation("Product {ProductId} stock updated, cache invalidated", id);
        }

        return result;
    }

    // ============ PRIVATE HELPERS ============

    private async Task InvalidateProductCaches()
    {
        // Remove all product-related caches
        await _cache.RemoveByPatternAsync(CachingExtensions.GenerateCachePattern(CacheKeys.Products));
        await _cache.RemoveByPatternAsync(CachingExtensions.GenerateCachePattern(CacheKeys.ProductsByCategory));
        await _cache.RemoveByPatternAsync(CachingExtensions.GenerateCachePattern(CacheKeys.ProductsFeatured));
    }

    private bool IsDefaultFilter(ProductFilterDTO filter)
    {
        return filter.CategoryId == null &&
               string.IsNullOrEmpty(filter.SearchTerm) &&
               string.IsNullOrEmpty(filter.Brand) &&
               filter.MinPrice == null &&
               filter.MaxPrice == null &&
               filter.IsFeatured == null &&
               filter.IsAvailable == null;
    }
}
