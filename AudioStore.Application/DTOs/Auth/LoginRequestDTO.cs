namespace AudioStore.Application.DTOs.Auth;

public class LoginRequestDTO
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

}
