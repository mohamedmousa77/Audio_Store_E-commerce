namespace AudioStore.Common.DTOs.Profile;

public record SaveAddressDTO
{
    public int? AddressId { get; init; } // to updating existing address. Null per nuovo indirizzo 
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public bool SetAsDefault { get; init; }

}
