using Asp.Versioning;
using AudioStore.Common.DTOs.Auth;
using AudioStore.Common.Services.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AudioStore.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _jwtTokenService;
    private readonly IMediator _mediator;


    public AuthController(IAuthService authService, ITokenService jwtTokenService, IMediator mediator)
    {
        _authService = authService;
        _mediator = mediator;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDTO request)
    {
        // Validazione tramite MediatorR
        //var result = await _mediator.Send(command);

        //  Validazione manuale
        //var validationResult = await validator.ValidateAsync(request);
        //if (!validationResult.IsValid)
        //{
        //    return BadRequest(new
        //    {
        //        errors = validationResult.Errors.Select(e => new
        //        {
        //            field = e.PropertyName,
        //            message = e.ErrorMessage
        //        })
        //    });
        //}


        var result = await _authService.RegisterAsync(request);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error, errorCode = result.ErrorCode });

        return Ok(result.Value);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDTO request)
    {
        var result = await _authService.LoginAsync(request);

        if (result.IsFailure)
            return Unauthorized(new { error = result.Error, errorCode = result.ErrorCode });

        return Ok(result.Value);
    }

    //  NUOVO: Endpoint refresh token
    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDTO request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _jwtTokenService.RefreshTokenAsync(request.RefreshToken, ipAddress);

        if (result.IsFailure)
            return Unauthorized(new { error = result.Error });

        return Ok(result.Value);
    }

    //  NUOVO: Endpoint revoca token
    [HttpPost("revoke-token")]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequestDTO request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var result = await _jwtTokenService.RevokeTokenAsync(request.RefreshToken, ipAddress);

        if (result.IsFailure)
            return BadRequest(new { error = result.Error });

        return Ok(new { message = "Token revocato con successo" });
    }

}
