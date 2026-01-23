using AudioStore.Domain.Entities;
using AudioStore.Domain.Enums;

namespace AudioStore.Domain.Interfaces;

public interface IDashboardRepository : IRepository<Order>
{
    // ============ DASHBOARD QUERIES ============
    Task<decimal> GetTotalSalesAsync();
    Task<Dictionary<OrderStatus, int>> GetOrdersByStatusAsync();
    Task<IEnumerable<TopProductData>> GetTopProductsAsync(int count = 5);
    Task<IEnumerable<TopCategoryData>> GetTopCategoriesAsync(int count = 3);
}

//  Helper classes per evitare tipi anonimi
public class TopProductData
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class TopCategoryData
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
}