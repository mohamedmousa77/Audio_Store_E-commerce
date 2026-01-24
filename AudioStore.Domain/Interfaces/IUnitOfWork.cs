using AudioStore.Domain.Entities;

namespace AudioStore.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    // Specific Repositories
    IUserRepository Users { get; }
    IOrderRepository Orders { get; }
    IDashboardRepository Dashboard {  get; }
    IProductRepository Products { get; }
    ICartRepository Carts { get; }
    ICartItemsRepository CartItems { get; }

    //Generic Repositories
    IRepository<Category> Categories { get; }    
    IRepository<OrderItem> OrderItems { get; }
    IRepository<Address> Addresses { get; }
    IRepository<RefreshToken> RefreshTokens { get; }

    // Transaction Management
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
