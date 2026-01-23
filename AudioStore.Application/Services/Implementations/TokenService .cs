using AudioStore.Application.DTOs.Auth;
using AudioStore.Application.Services.Implementations;
using AudioStore.Common.Constants;
using AudioStore.Common.Result;
using AudioStore.Domain;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AudioStore.Application;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly UserManager<User> _userManager;
    private readonly ILogger<AuthService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public TokenService(IOptions<JwtSettings> jwtSettings, UserManager<User> userManager, IUnitOfWork unitOfWork, ILogger<AuthService> logger)
    {
        _jwtSettings = jwtSettings.Value;
        _userManager = userManager;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<string> GenerateAccessTokenAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName!),
            new("FirstName", user.FirstName),
            new("LastName", user.LastName)
        };

        // Add roles
        var roles = await _userManager.GetRolesAsync(user);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSettings.Secret);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
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
            var newAccessToken = await GenerateAccessTokenAsync(tokenEntity.User);

            // 4. Genera nuovo refresh token (Token Rotation per sicurezza)
            var newRefreshToken = GenerateRefreshToken();

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

    // Revoca token manualmente
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
