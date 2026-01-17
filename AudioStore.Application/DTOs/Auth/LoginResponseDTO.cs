namespace AudioStore.Application.DTOs.Auth;

public class LoginResponseDTO
{
    public int UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public IEnumerable<string> Roles { get; init; } = new List<string>();
    public TokenResponseDTO Token { get; init; } = null!;

}
