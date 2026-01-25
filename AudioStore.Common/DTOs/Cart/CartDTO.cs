namespace AudioStore.Common.DTOs.Cart;

public record CartDTO
{
    public int Id { get; init; }
    public int? UserId { get; init; }
    public string? SessionId { get; init; }
    public List<CartItemDTO> Items { get; init; } = new();
    public decimal Subtotal { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal Tax { get; init; }
    public decimal TotalAmount { get; init; }
    public int TotalItems { get; init; }
    public bool IsGuestCart { get; init; }
}


