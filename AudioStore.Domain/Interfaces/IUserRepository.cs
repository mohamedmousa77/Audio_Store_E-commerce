using AudioStore.Domain.Entities;
using System.Linq.Expressions;

namespace AudioStore.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    //// Queries
    //Task<User?> GetByIdAsync(int id);
    //Task<User?> GetByEmailAsync(string email);
    //Task<User?> GetByUsernameAsync(string username);
    //Task<IEnumerable<User>> GetAllAsync();
    //Task<IEnumerable<User>> FindAsync(
    //    Expression<Func<User, bool>> predicate,
    //    CancellationToken cancellationToken = default);
    //Task<User?> FirstOrDefaultAsync(
    //    Expression<Func<User, bool>> predicate,
    //    CancellationToken cancellationToken = default);
    //Task<bool> AnyAsync(
    //    Expression<Func<User, bool>> predicate,
    //    CancellationToken cancellationToken = default);
    //Task<int> CountAsync(
    //    Expression<Func<User, bool>>? predicate = null,
    //    CancellationToken cancellationToken = default);

    //// Queries with Includes
    //IQueryable<User> Query();
    //IQueryable<User> QueryNoTracking();

    //// Commands
    //Task<User> AddAsync(User entity);
    //Task UpdateAsync(User entity);
    //Task DeleteAsync(int id);

    //  Queries Customer Management
    Task<User?> GetUserWithOrdersAsync(int userId);
    Task<User?> GetUserWithAddressesAsync(int userId);
    Task<User?> GetUserWithOrdersAndAddressesAsync(int userId);
    Task<IEnumerable<User>> GetCustomersWithOrdersAsync();
    Task<int> GetTotalCustomersCountAsync();
    Task<int> GetActiveCustomersThisMonthAsync();
}
