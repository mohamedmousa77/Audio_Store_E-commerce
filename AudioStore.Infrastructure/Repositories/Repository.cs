using AudioStore.Common.Result;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Linq.Expressions;

namespace AudioStore.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{

    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    // ============ QUERIES ============
    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Where(x => !x.IsDeleted)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet
            .Where(x => !x.IsDeleted)
            .ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> FirstOrDefaultAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        return predicate == null
            ? await _dbSet.CountAsync(cancellationToken)
            : await _dbSet.CountAsync(predicate, cancellationToken);
    }

    // Queryable for complex queries
    public virtual IQueryable<T> Query()
    {
        return _dbSet.AsQueryable();
    }

    public virtual IQueryable<T> QueryNoTracking()
    {
        return _dbSet.AsNoTracking();
    }

    // ============ COMMANDS ============
    public virtual async Task<T> AddAsync(T entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual async Task AddRangeAsync(
        IEnumerable<T> entities,
        CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public virtual async Task UpdateAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true; 
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
     public async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.AnyAsync(e => e.Id == id && !e.IsDeleted);
    }

    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

}
