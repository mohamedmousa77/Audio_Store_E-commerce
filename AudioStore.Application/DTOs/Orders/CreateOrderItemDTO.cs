namespace AudioStore.Application.DTOs.Orders;

public record CreateOrderItemDTO
{
    public int ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }

}
