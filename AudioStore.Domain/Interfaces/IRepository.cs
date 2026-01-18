using AudioStore.Common.Result;
using System.Linq.Expressions;

namespace AudioStore.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    // Queries
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);
    Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    // Queries with Includes
    IQueryable<T> Query();
    IQueryable<T> QueryNoTracking();

    // Commands
    Task<T> AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity);
    void UpdateRange(IEnumerable<T> entities);
    Task DeleteAsync(int id);
    void DeleteRange(IEnumerable<T> entities);
}
