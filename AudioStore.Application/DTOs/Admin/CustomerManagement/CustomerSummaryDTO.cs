namespace AudioStore.Application.DTOs.Admin.CustomerManagement;

public record CustomerSummaryDTO
{
    public int TotalCustomers { get; init; }
    public int ActiveCustomersThisMonth { get; init; }
    public int TotalOrders { get; init; }
    public TopCustomerDTO? TopCustomer { get; init; }

}
