using AudioStore.Domain.Entities;
using AudioStore.Domain.Enums;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AudioStore.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<User> _dbSet;

    public UserRepository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<User>();
    }

    // ============ BASIC CRUD FROM IRepository<User> ============
    
    public async Task<User?> GetByIdAsync(int id)
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> FindAsync(
        Expression<Func<User, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .Where(predicate)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> FirstOrDefaultAsync(
        Expression<Func<User, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<bool> AnyAsync(
        Expression<Func<User, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .AnyAsync(predicate, cancellationToken);
    }

    public async Task<int> CountAsync(
        Expression<Func<User, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(u => u.IsActive && !u.IsDeleted);
        
        return predicate == null
            ? await query.CountAsync(cancellationToken)
            : await query.CountAsync(predicate, cancellationToken);
    }

    public IQueryable<User> Query()
    {
        return _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .AsQueryable();
    }

    public IQueryable<User> QueryNoTracking()
    {
        return _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .AsNoTracking();
    }

    public async Task<User> AddAsync(User entity)
    {
        entity.RegistrationDate = DateTime.UtcNow;
        entity.CreatedAt = DateTime.UtcNow;
        entity.IsActive = true;
        entity.IsDeleted = false;
        
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public async Task AddRangeAsync(
        IEnumerable<User> entities,
        CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            entity.RegistrationDate = DateTime.UtcNow;
            entity.CreatedAt = DateTime.UtcNow;
            entity.IsActive = true;
            entity.IsDeleted = false;
        }
        
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public void Update(User entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    public void UpdateRange(IEnumerable<User> entities)
    {
        foreach (var entity in entities)
        {
            entity.UpdatedAt = DateTime.UtcNow;
        }
        _dbSet.UpdateRange(entities);
    }

    public void Delete(User entity)
    {
        // Soft delete
        entity.IsActive = false;
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    public void DeleteRange(IEnumerable<User> entities)
    {
        foreach (var entity in entities)
        {
            entity.IsActive = false;
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
        }
        _dbSet.UpdateRange(entities);
    }

    // ============ USER-SPECIFIC QUERIES ============
    
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .FirstOrDefaultAsync(u => u.UserName == username);
    }

    // ============ COMPLEX QUERIES WITH INCLUDES ============
    
    public async Task<User?> GetUserWithOrdersAsync(int userId)
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .Include(u => u.Orders.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserWithAddressesAsync(int userId)
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserWithOrdersAndAddressesAsync(int userId)
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .Include(u => u.Orders.Where(o => !o.IsDeleted))
                .ThenInclude(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    // ============ CUSTOMER MANAGEMENT QUERIES ============
    
    public async Task<IEnumerable<User>> GetCustomersWithOrdersAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .Where(u => u.Role == UserRole.Customer)
            .Include(u => u.Orders.Where(o => !o.IsDeleted))
            .ToListAsync();
    }

    public async Task<int> GetTotalCustomersCountAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .Where(u => u.Role == UserRole.Customer)
            .CountAsync();
    }

    public async Task<int> GetActiveCustomersThisMonthAsync()
    {
        var firstDayOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return await _context.Orders
            .Where(o => !o.IsDeleted)
            .Where(o => o.OrderDate >= firstDayOfMonth && o.UserId.HasValue)
            .Select(o => o.UserId!.Value)
            .Distinct()
            .CountAsync();
    }

    public async Task<User?> GetTopCustomerAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive && !u.IsDeleted)
            .Where(u => u.Role == UserRole.Customer)
            .Include(u => u.Orders.Where(o => !o.IsDeleted))
            .Where(u => u.Orders.Any())
            .OrderByDescending(u => u.Orders.Sum(o => o.TotalAmount))
            .FirstOrDefaultAsync();
    }
}
