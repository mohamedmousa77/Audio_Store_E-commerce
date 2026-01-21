namespace AudioStore.Application.DTOs.Profile;

public record UpdateProfileDTO
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string Email { get; init; }
}
