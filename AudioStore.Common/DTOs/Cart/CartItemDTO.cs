namespace AudioStore.Common.DTOs.Cart;

public record CartItemDTO
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductImage { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal Subtotal { get; init; }
    public int AvailableStock { get; init; }

}
