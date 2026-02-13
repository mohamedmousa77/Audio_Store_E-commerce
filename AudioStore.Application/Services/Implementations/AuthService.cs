using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Auth;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AudioStore.Application.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly ITokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        ITokenService jwtTokenService,
        ILogger<AuthService> logger,
        IUnitOfWork unitOfWork)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoginResponseDTO>> LoginAsync(LoginRequestDTO request)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
                return Result.Failure<LoginResponseDTO>("Credenziali non valide", ErrorCode.InvalidCredentials);

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
                return Result.Failure<LoginResponseDTO>("Credenziali non valide", ErrorCode.InvalidCredentials);

            if (!user.IsActive)
                return Result.Failure<LoginResponseDTO>("Account disattivato", ErrorCode.Unauthorized);

            //  Genera access token
            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
                user.Id, user.Email!, user.FirstName, user.LastName, user.Role);

            //  Genera e salva refresh token
            var refreshToken = _jwtTokenService.GenerateRefreshToken();
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = "127.0.0.1" // TODO: Get from HttpContext
            };

            await _unitOfWork.RefreshTokens.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var roles = await _userManager.GetRolesAsync(user);

            var response = new LoginResponseDTO
            {
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles,
                Token = new TokenResponseDTO
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(60)
                }
            };

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return Result.Failure<LoginResponseDTO>("Errore durante il login", ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<LoginResponseDTO>> RegisterAsync(RegisterRequestDTO request)
    {
        try
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
                return Result.Failure<LoginResponseDTO>(
                    "Email già registrata",
                    ErrorCode.EmailAlreadyExists);
            }

            var user = new User
            {
                UserName = request.Email,  // ✅ Required by ASP.NET Identity
                NormalizedUserName = request.Email.ToUpper(),  // ✅ Required
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpper(),  // ✅ Best practice
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                RegistrationDate = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = true  // ✅ Auto-confirm for now
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, errors);
                return Result.Failure<LoginResponseDTO>(errors, ErrorCode.ValidationError);
            }

            // Assign default Customer role
            await _userManager.AddToRoleAsync(user, UserRole.Customer);

            // Auto-login after registration
            var loginResponse = await LoginAsync(new LoginRequestDTO
            {
                Email = request.Email,
                Password = request.Password
            });

            _logger.LogInformation("User {UserId} registered successfully", user.Id);
            return loginResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email {Email}", request.Email);
            return Result.Failure<LoginResponseDTO>(
                "Errore durante la registrazione",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result> LogoutAsync(int userId)
    {
        try
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {UserId} logged out successfully", userId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return Result.Failure("Errore durante il logout", ErrorCode.InternalServerError);
        }
    }

}
