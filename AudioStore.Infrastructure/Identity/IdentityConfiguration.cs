using AudioStore.Domain.Entities;
using AudioStore.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AudioStore.Infrastructure.Identity;

/// <summary>
/// Extension methods for configuring ASP.NET Core Identity
/// </summary>
public static class IdentityConfiguration
{
    /// <summary>
    /// Adds and configures ASP.NET Core Identity with custom ApplicationRole
    /// </summary>
    public static IServiceCollection AddIdentityConfiguration(
        this IServiceCollection services)
    {
        // ✅ Identity Setup with ApplicationRole
        services.AddIdentity<User, ApplicationRole>(options =>
        {
            // ============ PASSWORD POLICY ============
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 4;

            // ============ LOCKOUT POLICY ============
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // ============ USER POLICY ============
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            // ============ SIGN-IN POLICY ============
            options.SignIn.RequireConfirmedEmail = false; // Set to true in production
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        //  Register Role Seeder
        services.AddScoped<RoleSeeder>();

        return services;
    }
}
