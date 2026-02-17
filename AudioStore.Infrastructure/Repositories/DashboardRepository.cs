using AudioStore.Common.Enums;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AudioStore.Infrastructure.Repositories;

public class DashboardRepository : Repository<Order>, IDashboardRepository
{
    public DashboardRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<decimal> GetTotalSalesAsync()
    {
        return await _dbSet
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SumAsync(o => o.TotalAmount);
    }

    public async Task<Dictionary<OrderStatus, int>> GetOrdersByStatusAsync()
    {
        return await _dbSet
            .GroupBy(o => o.Status)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task<IEnumerable<TopProductData>> GetTopProductsAsync(int count = 5)
    {
        return await _context.OrderItems
            .Include(oi => oi.Product)
            .ThenInclude(p => p.Category)
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.Status != OrderStatus.Cancelled)
            .GroupBy(oi => new
            {
                oi.ProductId,
                oi.Product.Name,
                oi.Product.MainImage,
                oi.Product.Brand,
                CategoryName = oi.Product.Category.Name,
                oi.Product.StockQuantity
            })
            .Select(g => new TopProductData
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                ProductImage = g.Key.MainImage,
                Brand = g.Key.Brand,
                CategoryName = g.Key.CategoryName,
                StockQuantity = g.Key.StockQuantity,
                TotalQuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Subtotal)
            })
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<TopCategoryData>> GetTopCategoriesAsync(int count = 3)
    {
        return await _context.OrderItems
            .Include(oi => oi.Product)
            .ThenInclude(p => p.Category)
            .Include(oi => oi.Order)
            .Where(oi => oi.Order.Status != OrderStatus.Cancelled)
            .GroupBy(oi => new { oi.Product.CategoryId, oi.Product.Category.Name })
            .Select(g => new TopCategoryData
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                TotalQuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Subtotal)
            })
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(count)
            .ToListAsync();
    }

}
