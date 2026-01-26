using Asp.Versioning;
using AudioStore.Common.DTOs.Orders;
using AudioStore.Common.DTOs.Profile;
using AudioStore.Common.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AudioStore.Api.Controllers;

/// <summary>
/// User profile management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IProfileService profileService,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserProfileDTO), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User must be authenticated" });
        }

        _logger.LogInformation("Getting profile for user: {UserId}", userId);

        var result = await _profileService.GetProfileAsync(userId.Value);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserProfileDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDTO dto)
    {
        var userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User must be authenticated" });
        }

        _logger.LogInformation("Updating profile for user: {UserId}", userId);

        var result = await _profileService.UpdateProfileAsync(userId.Value, dto);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
    {
        var userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User must be authenticated" });
        }

        _logger.LogInformation("Changing password for user: {UserId}", userId);

        var result = await _profileService.ChangePasswordAsync(userId.Value, dto);

        return result.IsSuccess
            ? Ok(new { message = "Password changed successfully" })
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get user addresses
    /// </summary>
    [HttpGet("addresses")]
    [ProducesResponseType(typeof(IEnumerable<AddressDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAddresses()
    {
        var userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User must be authenticated" });
        }

        _logger.LogInformation("Getting addresses for user: {UserId}", userId);

        var result = await _profileService.GetUserAddressesAsync(userId.Value);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Save address (create or update)
    /// </summary>
    [HttpPost("addresses")]
    [ProducesResponseType(typeof(AddressDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveAddress([FromBody] SaveAddressDTO dto)
    {
        var userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User must be authenticated" });
        }

        _logger.LogInformation("Saving address for user: {UserId}", userId);

        var result = await _profileService.SaveAddressAsync(userId.Value, dto);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetAddresses), new { }, result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Delete address
    /// </summary>
    [HttpDelete("addresses/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User must be authenticated" });
        }

        _logger.LogInformation("Deleting address {AddressId} for user: {UserId}", id, userId);

        var result = await _profileService.DeleteAddressAsync(userId.Value, id);

        return result.IsSuccess
            ? NoContent()
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get user order history
    /// </summary>
    [HttpGet("orders")]
    [ProducesResponseType(typeof(IEnumerable<OrderDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderHistory()
    {
        var userId = GetUserId();

        if (!userId.HasValue)
        {
            return Unauthorized(new { error = "User must be authenticated" });
        }

        _logger.LogInformation("Getting order history for user: {UserId}", userId);

        var result = await _profileService.GetUserOrdersAsync(userId.Value);

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
