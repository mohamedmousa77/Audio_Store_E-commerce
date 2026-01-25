namespace AudioStore.Common.DTOs.Auth;

public class TokenResponseDTO
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string TokenType { get; init; } = "Bearer";

}
