using AudioStore.Application.DTOs.Auth;
using AudioStore.Common.Result;

namespace AudioStore.Application.Services.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponseDTO>> LoginAsync(LoginRequestDTO request);
    Task<Result<LoginResponseDTO>> RegisterAsync(RegisterRequestDTO request);
    Task<Result<TokenResponseDTO>> RefreshTokenAsync(string refreshToken, string ipAddress);
    Task<Result> RevokeTokenAsync(string refreshToken, string ipAddress); 

    Task<Result> LogoutAsync(int userId);

}
