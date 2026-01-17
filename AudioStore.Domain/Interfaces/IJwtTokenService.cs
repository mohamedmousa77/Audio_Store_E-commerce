using AudioStore.Domain.Entities;
using System.Security.Claims;

namespace AudioStore.Domain.Interfaces;

public interface IJwtTokenService
{
    Task<string> GenerateAccessTokenAsync(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
