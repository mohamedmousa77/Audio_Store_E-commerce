namespace AudioStore.Common.DTOs.Admin.CustomerManagement;

public record CustomerListItemDTO
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime RegistrationDate { get; init; }
    public int TotalOrders { get; init; }
    public DateTime? LastOrderDate { get; init; }
    public decimal TotalSpent { get; init; }
}
