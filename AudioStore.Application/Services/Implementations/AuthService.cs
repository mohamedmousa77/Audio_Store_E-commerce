using AudioStore.Application.DTOs.Auth;
using AudioStore.Application.Services.Interfaces;
using AudioStore.Common.Constants;
using AudioStore.Common.Result;
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
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtTokenService jwtTokenService,
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

            // ✅ Genera access token
            var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user);

            // ✅ Genera e salva refresh token
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
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                RegistrationDate = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogWarning("Registration failed for {Email}: {Errors}", request.Email, errors);
                return Result.Failure<LoginResponseDTO>(errors, ErrorCode.ValidationError);
            }

            // Assign default role
            await _userManager.AddToRoleAsync(user, "Cliente");

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
    public async Task<Result<TokenResponseDTO>> RefreshTokenAsync(string refreshToken, string ipAddress)
    {
        try
        {
            // 1. Trova il refresh token nel database
            var tokenEntity = await _unitOfWork.RefreshTokens
                .Query()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (tokenEntity == null)
            {
                _logger.LogWarning("Refresh token not found: {Token}", refreshToken);
                return Result.Failure<TokenResponseDTO>("Token non valido", ErrorCode.InvalidToken);
            }

            // 2. Verifica se il token è ancora attivo
            if (!tokenEntity.IsActive)
            {
                _logger.LogWarning("Inactive refresh token used: {Token}", refreshToken);
                return Result.Failure<TokenResponseDTO>("Token scaduto o revocato", ErrorCode.InvalidToken);
            }

            // 3. Genera nuovo access token
            var newAccessToken = await _jwtTokenService.GenerateAccessTokenAsync(tokenEntity.User);

            // 4. Genera nuovo refresh token (Token Rotation per sicurezza)
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            // 5. Revoca il vecchio refresh token
            tokenEntity.IsRevoked = true;
            tokenEntity.RevokedAt = DateTime.UtcNow;
            tokenEntity.RevokedByIp = ipAddress;
            tokenEntity.ReplacedByToken = newRefreshToken;

            // 6. Salva il nuovo refresh token
            var newTokenEntity = new RefreshToken
            {
                UserId = tokenEntity.UserId,
                Token = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedByIp = ipAddress
            };

            await _unitOfWork.RefreshTokens.AddAsync(newTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var response = new TokenResponseDTO
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            };

            _logger.LogInformation("Token refreshed for user {UserId}", tokenEntity.UserId);
            return Result.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return Result.Failure<TokenResponseDTO>("Errore durante il refresh del token", ErrorCode.InternalServerError);
        }
    }

    // ✅ NUOVO: Revoca token manualmente
    public async Task<Result> RevokeTokenAsync(string refreshToken, string ipAddress)
    {
        try
        {
            var tokenEntity = await _unitOfWork.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (tokenEntity == null)
                return Result.Failure("Token non trovato", ErrorCode.InvalidToken);

            if (!tokenEntity.IsActive)
                return Result.Failure("Token già revocato", ErrorCode.InvalidToken);

            tokenEntity.IsRevoked = true;
            tokenEntity.RevokedAt = DateTime.UtcNow;
            tokenEntity.RevokedByIp = ipAddress;

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Token revoked for user {UserId}", tokenEntity.UserId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return Result.Failure("Errore durante la revoca del token", ErrorCode.InternalServerError);
        }
    }   

}
