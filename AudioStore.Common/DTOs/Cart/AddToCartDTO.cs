namespace AudioStore.Common.DTOs.Cart;

public record AddToCartDTO
{
    public int? UserId { get; init; }
    public string? SessionId { get; init; }
    public int ProductId { get; init; }
    public int Quantity { get; init; } = 1;
}
