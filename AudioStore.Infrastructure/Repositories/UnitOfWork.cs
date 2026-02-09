using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;

namespace AudioStore.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;

    //// Lazy initialization
    private IProductRepository? _products;    
    private IUserRepository? _users;
    private IOrderRepository? _orders;
    private IDashboardRepository? _boardRepository;
    private ICartRepository? _carts;
    private ICartItemsRepository? _cartItems;
    private IRepository<Category>? _categories;
    private IRepository<OrderItem>? _orderItems;
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
    public ICartRepository Carts => _carts ??= new CartRepository(_context);
    public ICartItemsRepository CartItems => _cartItems ??= new CartItemsRepository(_context);
    public IProductRepository Products => _products ??= new ProductRepository(_context);
    //public ICategoryRepository Categories => _categories ??= new CategoryRepository(_context);
    public IRepository<Category> Categories => _categories ??= new Repository<Category>(_context);
    public IRepository<Address> Addresses => _addresses ??= new Repository<Address>(_context);
    public IRepository<RefreshToken> RefreshTokens => _refreshTokens ??= new Repository<RefreshToken>(_context);

    // ============ TRANSACTION MANAGEMENT ============

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Detach all tracked entities from EF Core change tracker
    /// Useful when you need to force reload fresh data from database
    /// </summary>
    public void DetachAll()
    {
        var entries = _context.ChangeTracker.Entries()
            .Where(e => e.State != EntityState.Detached)
            .ToList();

        foreach (var entry in entries)
        {
            entry.State = EntityState.Detached;
        }
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
