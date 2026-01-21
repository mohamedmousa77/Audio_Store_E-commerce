using AudioStore.Application.DTOs.Auth;
using AudioStore.Common.Result;
using AudioStore.Domain.Entities;
using System.Security.Claims;

namespace AudioStore.Application;

public interface ITokenService
{
    Task<string> GenerateAccessTokenAsync(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
    Task<Result<TokenResponseDTO>> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<Result> RevokeTokenAsync(string refreshToken, string ipAddress);
}
