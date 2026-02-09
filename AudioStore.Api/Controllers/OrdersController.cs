using Asp.Versioning;
using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Orders;
using AudioStore.Common.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AudioStore.Api.Controllers;

/// <summary>
/// Orders management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        IOrderService orderService,
        ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's orders
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserOrders()
    {
        var userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User must be authenticated" });
        }

        _logger.LogInformation("Getting orders for user: {UserId}", userId);
        
        var result = await _orderService.GetUserOrdersAsync(userId.Value);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User must be authenticated" });
        }

        _logger.LogInformation("Getting order: {OrderId} for user: {UserId}", id, userId);
        
        var result = await _orderService.GetOrderByIdAsync(id);
        
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { error = result.Error });
        }

        // Verify user owns this order (unless admin)
        if (!User.IsInRole(UserRole.Admin) && result.Value!.UserId != userId.Value)
        {
            return Forbid();
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get order by order number
    /// Allows anonymous access for order confirmation
    /// </summary>
    [HttpGet("number/{orderNumber}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderByNumber(string orderNumber)
    {
        _logger.LogInformation("Getting order by number: {OrderNumber}", orderNumber);
        
        var result = await _orderService.GetOrderByNumberAsync(orderNumber);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Create new order from cart
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType(typeof(OrderConfirmationDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDTO dto)
    {
        var userId = GetUserId();

        // For authenticated users, bind the order to their user account.
        // For guests, leave UserId null and use customer info from the DTO.
        var createOrderDto = userId.HasValue
            ? dto with { UserId = userId.Value }
            : dto with { UserId = null };

        _logger.LogInformation("Creating order for user: {UserId}", userId);
        
        var result = await _orderService.CreateOrderAsync(createOrderDto);
        
        if (result.IsSuccess)
        {
            // OrderConfirmationDTO has OrderDetails.Id, not OrderId
            return CreatedAtAction(
                nameof(GetOrderById), 
                new { id = result.Value!.OrderDetails.Id }, 
                result.Value);
        }
        
        return StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Cancel order
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CancelOrder(int id)
    {
        var userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User must be authenticated" });
        }

        _logger.LogInformation("Canceling order: {OrderId} for user: {UserId}", id, userId);
        
        var result = await _orderService.CancelOrderAsync(id, userId.Value);
        
        return result.IsSuccess 
            ? Ok(new { message = "Order cancelled successfully" }) 
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get all orders with filtering (Admin only)
    /// </summary>
    [HttpGet("admin/all")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(PaginatedResult<OrderDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders([FromQuery] OrderFilterDTO filter)
    {
        _logger.LogInformation("Admin getting all orders");
        
        var result = await _orderService.GetAllOrdersAsync(filter);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Update order status (Admin only)
    /// </summary>
    [HttpPatch("{id}/status")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(OrderDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDTO dto)
    {
        if (id != dto.OrderId)
        {
            return BadRequest(new { error = "ID mismatch" });
        }

        _logger.LogInformation("Admin updating order {OrderId} status to: {Status}", id, dto.NewStatus);
        
        var result = await _orderService.UpdateOrderStatusAsync(dto);
        
        return result.IsSuccess 
            ? Ok(result.Value) 
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    #region Helper Methods

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    #endregion
}
