using AudioStore.Domain.Entities;

namespace AudioStore.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    // ============ ORDER-SPECIFIC QUERIES ============
    Task<Order?> GetOrderWithItemsAsync(int orderId);
    Task<Order?> GetOrderByNumberAsync(string orderNumber);
    Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);

    // ORDER-SPECIFIC QUERIES FOR ADMIN 
    Task<Order?> GetOrderById(int orderId);
}
