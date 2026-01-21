namespace AudioStore.Application.DTOs.Admin.CustomerManagement;

public record TopCustomerDTO
{
    public int UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public decimal TotalSpent { get; init; }
    public int TotalOrders { get; init; }

}
