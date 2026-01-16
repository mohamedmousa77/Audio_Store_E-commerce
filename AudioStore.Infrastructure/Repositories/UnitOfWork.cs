using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace AudioStore.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy initialization dei repositories
    private IRepository<Product>? _products;
    private IRepository<Category>? _categories;
    private IRepository<Order>? _orders;
    private IRepository<OrderItem>? _orderItems;
    private IRepository<Cart>? _carts;
    private IRepository<CartItem>? _cartItems;
    private IRepository<Address>? _addresses;
    private IRepository<User>? _users;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    // ============ REPOSITORIES (Lazy Loading) ============
    public IRepository<Product> Products
        => _products ??= new Repository<Product>(_context);

    public IRepository<Category> Categories
        => _categories ??= new Repository<Category>(_context);

    public IRepository<Order> Orders
        => _orders ??= new Repository<Order>(_context);

    public IRepository<OrderItem> OrderItems
        => _orderItems ??= new Repository<OrderItem>(_context);

    public IRepository<Cart> Carts
        => _carts ??= new Repository<Cart>(_context);

    public IRepository<CartItem> CartItems
        => _cartItems ??= new Repository<CartItem>(_context);

    public IRepository<Address> Addresses
        => _addresses ??= new Repository<Address>(_context);

    public IRepository<User> Users
        => _users ??= new Repository<User>(_context);

    // ============ TRANSACTION MANAGEMENT ============
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    // ============ DISPOSE ============
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
