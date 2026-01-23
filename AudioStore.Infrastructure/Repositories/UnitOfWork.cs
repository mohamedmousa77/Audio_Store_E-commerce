using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace AudioStore.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    //// Lazy initialization
    private IRepository<Product>? _products;
    private IRepository<Category>? _categories;
    private IUserRepository? _users;
    private IOrderRepository? _orders;
    private IDashboardRepository? _boardRepository;
    private IRepository<OrderItem>? _orderItems;
    private IRepository<Cart>? _carts;
    private IRepository<CartItem>? _cartItems;
    private IRepository<Address>? _addresses;
    private IRepository<RefreshToken>? _refreshTokens;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    // Repository Properties - Lazy Initialization
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public IOrderRepository Orders => _orders ??= new OrderRepository(_context);
    public IRepository<OrderItem> OrderItems => _orderItems ??= new Repository<OrderItem>(_context);
    public IDashboardRepository Dashboard => _boardRepository ??= new DashboardRepository(_context);

    //public IProductRepository Products => _products ??= new ProductRepository(_context);
    //public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public IRepository<Product> Products => _products ??= new Repository<Product>(_context);
    public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);
    public IRepository<Cart> Carts => _carts ??= new Repository<Cart>(_context);
    public IRepository<CartItem> CartItems => _cartItems ??= new Repository<CartItem>(_context);
    public IRepository<Address> Addresses => _addresses ??= new Repository<Address>(_context);
    public IRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new Repository<RefreshToken>(_context);

    // ============ TRANSACTION MANAGEMENT ============

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
