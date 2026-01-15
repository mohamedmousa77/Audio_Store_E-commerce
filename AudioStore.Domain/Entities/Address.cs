namespace AudioStore.Domain.Entities;

public class Address : BaseEntity
{
    public int? UserId { get; set; } // Nullable per Guest orders
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;

    // Navigation Properties
    public virtual User? User { get; set; }
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
