namespace AudioStore.Application.DTOs.Admin.Dashboard;

internal class TopProductDTO
{
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductImage { get; init; } = string.Empty;
    public string Brand {  get; init; } = string.Empty;
    public string Categoria {  get; init; } = string.Empty;
    public string StockStatus {  get; init; } = string.Empty;
    public int TotalQuantitySold { get; init; }
    public decimal TotalRevenue { get; init; }

}
