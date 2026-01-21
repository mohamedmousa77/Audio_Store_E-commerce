using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace AudioStore.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    //// Lazy initialization
    private IRepository<Product>? _products;
    private IRepository<Category>? _categories;
    public IUserRepository? Users { get; }
    public IOrderRepository? Orders { get; }
    private IRepository<OrderItem>? _orderItems;
    private IRepository<Cart>? _carts;
    private IRepository<CartItem>? _cartItems;
    private IRepository<Address>? _addresses;
    private IUserRepository? _users;
    private IRepository<RefreshToken>? _refreshTokens;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        // ✅ Inizializza repository specifici
        //Users = new UserRepository(_context);
        //Products = new ProductRepository(_context);
        //Categories = new CategoryRepository(_context);
        //Orders = new OrderRepository(_context);

        //// Inizializza repository generici
        //OrderItems = new Repository<OrderItem>(_context);
        //Carts = new Repository<Cart>(_context);
        //CartItems = new Repository<CartItem>(_context);
        //Addresses = new Repository<Address>(_context);
    }

    // Repositories
    public IRepository<Product> Products => _products ??= new Repository<Product>(_context);
    public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);
    public IOrderRepository Orders => _orders ??= new IOrderRepository(_context);
    public IRepository<OrderItem> OrderItems => _orderItems ??= new Repository<OrderItem>(_context);
    public IRepository<Cart> Carts => _carts ??= new Repository<Cart>(_context);
    public IRepository<CartItem> CartItems => _cartItems ??= new Repository<CartItem>(_context);
    public IRepository<Address> Addresses => _addresses ??= new Repository<Address>(_context);
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IRepository<RefreshToken> RefreshTokens  
       => _refreshTokens ??= new Repository<RefreshToken>(_context);

    // Transaction Management
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
