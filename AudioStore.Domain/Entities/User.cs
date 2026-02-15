using Microsoft.AspNetCore.Identity;

namespace AudioStore.Domain.Entities;

public class User : IdentityUser<int>
{

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    // Role property removed - ASP.NET Identity uses UserRoles table instead

    // Le proprieta` di BaseEntity:
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public int? CreatedById { get; set; }
    public int? ModifiedById { get; set; }

    // Navigation Properties
    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual Cart? Cart { get; set; }

    // Computed Property
    public string FullName => $"{FirstName} {LastName}";
}
