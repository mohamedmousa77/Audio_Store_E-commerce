using AudioStore.Common.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace AudioStore.Infrastructure.Identity;

/// <summary>
/// Service responsible for seeding application roles
/// </summary>
public class RoleSeeder
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ILogger<RoleSeeder> _logger;

    public RoleSeeder(
        RoleManager<ApplicationRole> roleManager,
        ILogger<RoleSeeder> logger)
    {
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <summary>
    /// Seeds all system roles defined in UserRole constants
    /// </summary>
    public async Task SeedRolesAsync()
    {
        var roles = new[]
        {
            new ApplicationRole
            {
                Name = UserRole.Admin,
                Description = "Administrator with full system access",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            },
            new ApplicationRole
            {
                Name = UserRole.Customer,
                Description = "Customer with e-commerce access",
                IsSystemRole = true,
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role.Name!))
            {
                var result = await _roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    _logger.LogInformation(
                        "Role '{RoleName}' created successfully",
                        role.Name);
                }
                else
                {
                    _logger.LogError(
                        "Failed to create role '{RoleName}': {Errors}",
                        role.Name,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation(
                    "Role '{RoleName}' already exists",
                    role.Name);
            }
        }
    }
}
