namespace AudioStore.Application.DTOs.Profile;

public record AddressDTO
{
    public int Id { get; init; }
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public bool IsDefault { get; init; }

}
