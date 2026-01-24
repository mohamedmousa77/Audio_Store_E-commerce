using Microsoft.AspNetCore.Identity;

namespace AudioStore.Infrastructure.Identity;

/// <summary>
/// Custom application role with additional metadata
/// </summary>
public class ApplicationRole : IdentityRole<int>
{
    /// <summary>
    /// Description of the role's purpose and permissions
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When this role was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// System roles cannot be deleted (Admin, Customer)
    /// </summary>
    public bool IsSystemRole { get; set; } = false;

    /// <summary>
    /// When this role was last modified
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
