using AudioStore.Domain.Entities;

namespace AudioStore.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Repositories
    IRepository<Product> Products { get; }
    IRepository<Category> Categories { get; }
    IRepository<Order> Orders { get; }
    IRepository<OrderItem> OrderItems { get; }
    IRepository<Cart> Carts { get; }
    IRepository<CartItem> CartItems { get; }
    IRepository<Address> Addresses { get; }
    IRepository<User> Users { get; }

    // Transaction Management
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
