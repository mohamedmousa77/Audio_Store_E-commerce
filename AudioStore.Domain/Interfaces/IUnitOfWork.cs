using AudioStore.Domain.Entities;

namespace AudioStore.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Repositories
    IRepository<Product> Products { get; }
    IRepository<Category> Categories { get; }
    IOrderRepository Orders { get; }
    IRepository<OrderItem> OrderItems { get; }
    IRepository<Cart> Carts { get; }
    IRepository<CartItem> CartItems { get; }
    IRepository<Address> Addresses { get; }
    IUserRepository Users { get; }
    IRepository<RefreshToken> RefreshTokens { get; }

    // Transaction Management
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
