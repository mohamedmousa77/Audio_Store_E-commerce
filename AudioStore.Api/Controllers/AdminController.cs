using Asp.Versioning;
using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Admin.CustomerManagement;
using AudioStore.Common.DTOs.Admin.Dashboard;
using AudioStore.Common.DTOs.Orders;
using AudioStore.Common.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AudioStore.Api.Controllers;

/// <summary>
/// Admin management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = UserRole.Admin)]
public class AdminController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICustomerManagementService _customerManagementService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IDashboardService dashboardService,
        ICustomerManagementService customerManagementService,
        ILogger<AdminController> logger)
    {
        _dashboardService = dashboardService;
        _customerManagementService = customerManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Get dashboard statistics
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardStatsDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboardStats()
    {
        _logger.LogInformation("Admin getting dashboard statistics");
        
        var result = await _dashboardService.GetDashboardStatsAsync();
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get customer summary/overview
    /// </summary>
    [HttpGet("customers/summary")]
    [ProducesResponseType(typeof(CustomerSummaryDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerSummary()
    {
        _logger.LogInformation("Admin getting customer summary");
        
        var result = await _customerManagementService.GetCustomerSummaryAsync();
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get all customers with pagination and filtering
    /// </summary>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(PaginatedResult<CustomerListItemDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomers([FromQuery] CustomerFilterDTO filter)
    {
        _logger.LogInformation("Admin getting customers");
        
        var result = await _customerManagementService.GetCustomersAsync(filter);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get customer details by ID
    /// </summary>
    [HttpGet("customers/{id}")]
    [ProducesResponseType(typeof(CustomerDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerDetail(int id)
    {
        _logger.LogInformation("Admin getting customer details: {CustomerId}", id);
        
        var result = await _customerManagementService.GetCustomerDetailAsync(id);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get customer order history
    /// </summary>
    [HttpGet("customers/{id}/orders")]
    [ProducesResponseType(typeof(IEnumerable<OrderDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomerOrderHistory(int id)
    {
        _logger.LogInformation("Admin getting order history for customer: {CustomerId}", id);
        
        var result = await _customerManagementService.GetCustomerOrderHistoryAsync(id);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
