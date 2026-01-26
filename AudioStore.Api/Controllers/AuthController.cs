using Asp.Versioning;
using AudioStore.Common.DTOs.Auth;
using AudioStore.Common.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AudioStore.Api.Controllers;

/// <summary>
/// Authentication and authorization endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenServices;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        ITokenService tokenServices,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _tokenServices = tokenServices;
        _logger = logger;
    }

    /// <summary>
    /// Register new user
    /// </summary>
    /// <param name="request">Registration data</param>
    /// <returns>Login response with JWT tokens</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
    {
        _logger.LogInformation("User registration attempt for email: {Email}", request.Email);

        var result = await _authService.RegisterAsync(request);

        if (result.IsFailure)
        {
            _logger.LogWarning("Registration failed for {Email}: {Error}", request.Email, result.Error);
            return BadRequest(new
            {
                error = result.Error,
                errorCode = result.ErrorCode
            });
        }

        _logger.LogInformation("User registered successfully: {Email}", request.Email);
        return Ok(result.Value);
    }

    /// <summary>
    /// User login
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>Login response with JWT tokens</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var result = await _authService.LoginAsync(request);

        if (result.IsFailure)
        {
            _logger.LogWarning("Login failed for {Email}: {Error}", request.Email, result.Error);
            return Unauthorized(new
            {
                error = result.Error,
                errorCode = result.ErrorCode
            });
        }

        _logger.LogInformation("User logged in successfully: {Email}", request.Email);
        return Ok(result.Value);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New access token and refresh token</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation("Refresh token attempt from IP: {IpAddress}", ipAddress);

        var result = await _tokenServices.RefreshTokenAsync(request.RefreshToken, ipAddress);

        if (result.IsFailure)
        {
            _logger.LogWarning("Refresh token failed from IP {IpAddress}: {Error}", ipAddress, result.Error);
            return Unauthorized(new { error = result.Error });
        }

        _logger.LogInformation("Token refreshed successfully from IP: {IpAddress}", ipAddress);
        return Ok(result.Value);
    }

    /// <summary>
    /// Revoke refresh token (logout)
    /// </summary>
    /// <param name="request">Token to revoke</param>
    /// <returns>Success message</returns>
    [HttpPost("revoke-token")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDTO request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        _logger.LogInformation("Revoke token attempt from IP: {IpAddress}", ipAddress);

        var result = await _tokenServices.RevokeTokenAsync(request.RefreshToken, ipAddress);

        if (result.IsFailure)
        {
            _logger.LogWarning("Revoke token failed from IP {IpAddress}: {Error}", ipAddress, result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("Token revoked successfully from IP: {IpAddress}", ipAddress);
        return Ok(new { message = "Token revoked successfully" });
    }

    /// <summary>
    /// Logout current user
    /// </summary>
    /// <returns>Success message</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var userEmail = User.Identity?.Name;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { error = "Invalid user ID" });
        }

        _logger.LogInformation("Logout attempt for user: {Email}", userEmail);

        var result = await _authService.LogoutAsync(userId);

        if (result.IsFailure)
        {
            _logger.LogWarning("Logout failed for {Email}: {Error}", userEmail, result.Error);
            return BadRequest(new { error = result.Error });
        }

        _logger.LogInformation("User logged out successfully: {Email}", userEmail);
        return Ok(new { message = "Logged out successfully" });
    }
}
