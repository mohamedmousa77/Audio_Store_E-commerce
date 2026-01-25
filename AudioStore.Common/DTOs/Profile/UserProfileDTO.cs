namespace AudioStore.Common.DTOs.Profile;

public record UserProfileDTO
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public DateTime RegistrationDate { get; init; }

    // Default Address (if exists)
    public AddressDTO? DefaultAddress { get; init; }

}
