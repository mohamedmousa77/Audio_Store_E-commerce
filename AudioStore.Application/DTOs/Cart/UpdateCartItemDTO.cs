namespace AudioStore.Application.DTOs.Cart;

public record UpdateCartItemDTO
{
    public int CartItemId { get; init; }
    public int Quantity { get; init; }
}
