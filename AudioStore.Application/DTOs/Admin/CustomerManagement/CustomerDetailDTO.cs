using AudioStore.Application.DTOs.Orders;

namespace AudioStore.Application.DTOs.Admin.CustomerManagement;

public record CustomerDetailDTO
{
    // Personal Info
    public int UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public DateTime RegistrationDate { get; init; }

    // Order Statistics
    public int TotalOrders { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal AverageOrderAmount { get; init; }
    public DateTime? LastOrderDate { get; init; }

    // Recent Orders
    public List<OrderDTO> RecentOrders { get; init; } = new();
}
