namespace AudioStore.Application.DTOs.Auth;

public class RevokeTokenRequestDTO
{
    public string RefreshToken { get; init; } = string.Empty;

}
