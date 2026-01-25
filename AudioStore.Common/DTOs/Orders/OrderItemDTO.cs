namespace AudioStore.Common.DTOs.Orders;

public record OrderItemDTO
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductImage { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }

}
