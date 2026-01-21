using AudioStore.Domain.Entities;
using AudioStore.Domain.Enums;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AudioStore.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    //protected readonly AppDbContext _context;
    //protected readonly DbSet<User> _dbSet;

    public UserRepository(AppDbContext context) : base(context)
    {
        //_context = context;
        //_dbSet = context.Set<User>();
    }

    // ============ QUERIES ============
    public virtual async Task<User?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public virtual async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync(u => u.UserName == username);
    }

    public virtual async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _dbSet
            .Where(x => x.IsActive)
            .ToListAsync();
    }

    public virtual async Task<IEnumerable<User>> FindAsync(
        Expression<Func<User, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.IsActive)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<User?> FirstOrDefaultAsync(
        Expression<Func<User, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.IsActive)
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public virtual async Task<bool> AnyAsync(
        Expression<Func<User, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(x => x.IsActive)
            .AnyAsync(predicate, cancellationToken);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<User, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(x => x.IsActive);

        return predicate == null
            ? await query.CountAsync(cancellationToken)
            : await query.CountAsync(predicate, cancellationToken);
    }

    // Queryable for complex queries
    public virtual IQueryable<User> Query()
    {
        return _dbSet.Where(x => x.IsActive).AsQueryable();
    }

    public virtual IQueryable<User> QueryNoTracking()
    {
        return _dbSet.Where(x => x.IsActive).AsNoTracking();
    }

    // ============ COMMANDS ============
    public virtual async Task<User> AddAsync(User entity)
    {
        entity.RegistrationDate = DateTime.UtcNow;
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual async Task UpdateAsync(User entity)
    {
        _dbSet.Update(entity);
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            entity.IsActive = false; // Soft delete using IsActive
        }
    }

    // Queries Complecated. 
    public async Task<User?> GetUserWithOrdersAsync(int userId)
    {
        return await _dbSet
            .Include(u => u.Orders.Where(o => !o.IsDeleted))
            .ThenInclude(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserWithAddressesAsync(int userId)
    {
        return await _dbSet
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserWithOrdersAndAddressesAsync(int userId)
    {
        return await _dbSet
            .Include(u => u.Orders.Where(o => !o.IsDeleted))
            .ThenInclude(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }



    public async Task<IEnumerable<User>> GetCustomersWithOrdersAsync()
    {
        return await _dbSet
            .Include(u => u.Orders.Where(o => !o.IsDeleted))
            .Where(u => u.Role == UserRole.Customer)
            .ToListAsync();
    }

    public async Task<int> GetTotalCustomersCountAsync()
    {
        return await _dbSet
            .Where(u => u.Role == UserRole.Customer)
            .CountAsync();
    }

    public async Task<int> GetActiveCustomersThisMonthAsync()
    {
        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return await _context.Orders
            .Where(o => o.OrderDate >= firstDayOfMonth && o.UserId.HasValue)
            .Select(o => o.UserId!.Value)
            .Distinct()
            .CountAsync();
    }

}
