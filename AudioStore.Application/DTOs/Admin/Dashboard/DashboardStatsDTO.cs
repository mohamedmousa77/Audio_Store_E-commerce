namespace AudioStore.Application.DTOs.Admin.Dashboard;

public record DashboardStatsDTO
{
    // Sales
    public decimal TotalSales { get; init; }

    // Orders
    public int TotalOrders { get; init; }
    public OrdersByStatusDTO OrdersByStatus { get; init; } = new();

    // Customers
    public int TotalCustomers { get; init; }

    // Top Products
    public List<TopProductDTO> TopProducts { get; init; } = new();

    // Top Categories
    public List<TopCategoryDTO> TopCategories { get; init; } = new();

}
