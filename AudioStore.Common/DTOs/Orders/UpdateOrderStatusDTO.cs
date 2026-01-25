using AudioStore.Common.Enums;

namespace AudioStore.Common.DTOs.Orders;

public record UpdateOrderStatusDTO
{
    public int OrderId { get; init; }
    public OrderStatus NewStatus { get; init; }
}
