namespace AudioStore.Common.DTOs.Auth;

public class RevokeTokenRequestDTO
{
    public string RefreshToken { get; init; } = string.Empty;

}
