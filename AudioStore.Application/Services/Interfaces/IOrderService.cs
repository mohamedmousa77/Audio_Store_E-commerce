using AudioStore.Application.DTOs.Orders;
using AudioStore.Common.Result;

namespace AudioStore.Application.Services.Interfaces;

public interface IOrderService
{
    // ✅ Create Order
    Task<Result<OrderConfirmationDTO>> CreateOrderAsync(CreateOrderDTO dto);

    // ✅ Get Orders (for customers)
    Task<Result<OrderDTO>> GetOrderByIdAsync(int orderId);
    Task<Result<OrderDTO>> GetOrderByNumberAsync(string orderNumber);
    Task<Result<IEnumerable<OrderDTO>>> GetUserOrdersAsync(int userId);

    // ✅ Get Orders (for admin)
    Task<Result<PaginatedResult<OrderDTO>>> GetAllOrdersAsync(OrderFilterDTO filter);

    // ✅ Update Order Status (admin)
    Task<Result<OrderDTO>> UpdateOrderStatusAsync(UpdateOrderStatusDTO dto);

    // ✅ Cancel Order
    Task<Result> CancelOrderAsync(int orderId, int? userId = null);
}
