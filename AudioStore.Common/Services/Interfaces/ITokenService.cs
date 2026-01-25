using AudioStore.Common.DTOs.Auth;
using System.Security.Claims;

namespace AudioStore.Common.Services.Interfaces;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(
        int userId, string email, string firstName, string lastName, string role);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    Task<Result<TokenResponseDTO>> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<Result> RevokeTokenAsync(string refreshToken, string ipAddress);
}
