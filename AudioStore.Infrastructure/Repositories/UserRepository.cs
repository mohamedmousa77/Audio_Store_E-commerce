using AudioStore.Domain.Entities;
using AudioStore.Domain.Enums;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AudioStore.Infrastructure.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    // ============ USER-SPECIFIC QUERIES ============
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .FirstOrDefaultAsync(u => u.FullName == username);
    }

    // ============ COMPLEX QUERIES WITH INCLUDES ============
    public async Task<User?> GetUserWithOrdersAsync(int userId)
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .Include(u => u.Orders.Where(o => !o.IsDeleted))
            .ThenInclude(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserWithAddressesAsync(int userId)
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .Include(u => u.Addresses)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<User?> GetUserWithOrdersAndAddressesAsync(int userId)
    {
        return await _dbSet
            .Where(u => u.IsActive)
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
            .Where(u => u.IsActive)
            .Include(u => u.Orders.Where(o => !o.IsDeleted))
            .Where(u => u.Role == UserRole.Customer)
            .ToListAsync();
    }

    public async Task<int> GetTotalCustomersCountAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive && u.Role == UserRole.Customer)
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

    // TOP CUSTOMER
    public async Task<User?> GetTopCustomerAsync()
    {
        return await _dbSet
            .Where(u => u.IsActive)
            .Include(u => u.Orders.Where(o => !o.IsDeleted))
            .Where(u => u.Role == UserRole.Customer && u.Orders.Any())
            .OrderByDescending(u => u.Orders.Sum(o => o.TotalAmount))
            .FirstOrDefaultAsync();
    }
}
