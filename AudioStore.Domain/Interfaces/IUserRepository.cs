using AudioStore.Domain.Entities;

namespace AudioStore.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    // ============ USER-SPECIFIC QUERIES ============
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);

    // ============ COMPLEX QUERIES WITH INCLUDES ============
    Task<User?> GetUserWithOrdersAsync(int userId);
    Task<User?> GetUserWithAddressesAsync(int userId);
    Task<User?> GetUserWithOrdersAndAddressesAsync(int userId);

    // ============ CUSTOMER MANAGEMENT QUERIES ============
    Task<IEnumerable<User>> GetCustomersWithOrdersAsync();
    Task<int> GetTotalCustomersCountAsync();
    Task<int> GetActiveCustomersThisMonthAsync();

    //  TOP CUSTOMER - OTTIMIZZATO CON SQL
    Task<User?> GetTopCustomerAsync();
}
