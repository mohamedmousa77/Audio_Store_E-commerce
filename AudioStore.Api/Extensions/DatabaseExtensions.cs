using AudioStore.Domain.Entities;
using AudioStore.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace AudioStore.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            await DbInitializer.InitializeAsync(context, userManager, roleManager, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }
}
