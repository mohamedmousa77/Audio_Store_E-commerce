using Asp.Versioning;
using AudioStore.Common.DTOs.Cart;
using AudioStore.Common.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AudioStore.Api.Controllers;

/// <summary>
/// Shopping cart management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(
        ICartService cartService,
        ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's cart
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        _logger.LogInformation("Getting cart for user: {UserId} or session: {SessionId}", userId, sessionId);

        var result = await _cartService.GetOrCreateCartAsync(userId, sessionId);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Add item to cart
    /// </summary>
    [HttpPost("items")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem([FromBody] AddToCartDTO dto)
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        // Create new DTO with userId and sessionId (init-only properties)
        var addToCartDto = dto with { UserId = userId, SessionId = sessionId };

        _logger.LogInformation("Adding item to cart - Product: {ProductId}, Quantity: {Quantity}",
            addToCartDto.ProductId, addToCartDto.Quantity);

        var result = await _cartService.AddItemAsync(addToCartDto);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Update cart item quantity
    /// </summary>
    [HttpPut("items/{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItem(int id, [FromBody] UpdateCartItemDTO dto)
    {
        if (id != dto.CartItemId)
        {
            return BadRequest(new { error = "ID mismatch" });
        }

        _logger.LogInformation("Updating cart item: {CartItemId} to quantity: {Quantity}",
            id, dto.Quantity);

        var result = await _cartService.UpdateItemQuantityAsync(dto);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Remove item from cart
    /// </summary>
    [HttpDelete("items/{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(int id)
    {
        _logger.LogInformation("Removing cart item: {CartItemId}", id);

        var result = await _cartService.RemoveItemAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Clear all items from cart
    /// </summary>
    [HttpDelete("clear")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetUserId();
        var sessionId = GetSessionId();

        _logger.LogInformation("Clearing cart for user: {UserId} or session: {SessionId}", userId, sessionId);

        var result = await _cartService.ClearCartAsync(userId, sessionId);

        return result.IsSuccess
            ? Ok(new { message = "Cart cleared successfully" })
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Merge guest cart to user cart (called on login)
    /// </summary>
    [HttpPost("merge")]
    [ProducesResponseType(typeof(CartDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MergeCart([FromBody] string sessionId)
    {
        var userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User must be authenticated" });
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest(new { error = "Session ID is required" });
        }

        _logger.LogInformation("Merging guest cart {SessionId} to user cart {UserId}",
            sessionId, userId);

        var result = await _cartService.MergeGuestCartToUserAsync(sessionId, userId.Value);

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

    private string? GetSessionId()
    {
        return HttpContext.Request.Headers["X-Session-Id"].FirstOrDefault()
            ?? HttpContext.Session.Id;
    }

    #endregion
}
