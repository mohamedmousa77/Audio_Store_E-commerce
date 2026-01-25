using AudioStore.Common.DTOs.Admin.CustomerManagement;
using AudioStore.Common.DTOs.Orders;

namespace AudioStore.Common.Services.Interfaces;

public interface ICustomerManagementService
{
    // Summary/Overview
    Task<Result<CustomerSummaryDTO>> GetCustomerSummaryAsync();

    // Customer List
    Task<Result<PaginatedResult<CustomerListItemDTO>>> GetCustomersAsync(CustomerFilterDTO filter);

    // Customer Details
    Task<Result<CustomerDetailDTO>> GetCustomerDetailAsync(int userId);

    // Customer Orders History
    Task<Result<IEnumerable<OrderDTO>>> GetCustomerOrderHistoryAsync(int userId);
}
