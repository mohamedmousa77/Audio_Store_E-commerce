using AudioStore.Common.DTOs.Orders;

namespace AudioStore.Common.DTOs.Admin.CustomerManagement;

public record CustomerDetailDTO
{
    // Personal Info
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public DateTime RegistrationDate { get; init; }

    public string Status { get; init; } = "Active";
    // Order Statistics
    public int TotalOrders { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal AverageOrderAmount { get; init; }
    public DateTime? LastOrderDate { get; init; }

    // Recent Orders
    public List<OrderDTO> RecentOrders { get; init; } = new();
}
