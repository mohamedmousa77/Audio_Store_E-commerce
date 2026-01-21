namespace AudioStore.Application.DTOs.Admin.Dashboard;

public record TopCategoryDTO
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public int TotalQuantitySold { get; init; }
    public decimal TotalRevenue { get; init; }
}
