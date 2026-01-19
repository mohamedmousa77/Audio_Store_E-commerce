using AudioStore.Domain.Enums;

namespace AudioStore.Application.DTOs.Orders;

public record UpdateOrderStatusDTO
{
    public int OrderId { get; init; }
    public OrderStatus NewStatus { get; init; }
}
