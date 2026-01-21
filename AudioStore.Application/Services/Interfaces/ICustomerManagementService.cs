using AudioStore.Application.DTOs.Admin.CustomerManagement;
using AudioStore.Application.DTOs.Orders;
using AudioStore.Common.Result;

namespace AudioStore.Application.Services.Interfaces;

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
