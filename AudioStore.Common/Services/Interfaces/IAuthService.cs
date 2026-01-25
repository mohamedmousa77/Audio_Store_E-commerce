using AudioStore.Common.DTOs.Auth;

namespace AudioStore.Common.Services.Interfaces;

public interface IAuthService
{
    Task<Result<LoginResponseDTO>> LoginAsync(LoginRequestDTO request);
    Task<Result<LoginResponseDTO>> RegisterAsync(RegisterRequestDTO request);
    Task<Result> LogoutAsync(int userId);

}
