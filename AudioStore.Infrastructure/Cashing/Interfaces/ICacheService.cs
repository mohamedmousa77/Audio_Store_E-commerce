namespace AudioStore.Infrastructure.Cashing.Interfaces;

public interface ICacheService
{
    /// Get cached value by key
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    /// Set cache value with expiration
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    
    /// Get or create cached value
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default);

    /// Remove cached value
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// Remove all keys matching pattern
    Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// Check if key exists
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    
    /// Clear all cache
    Task ClearAsync(CancellationToken cancellationToken = default);
}
