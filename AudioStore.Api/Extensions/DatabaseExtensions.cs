using AudioStore.Infrastructure.Data;
using AudioStore.Infrastructure.Identity;
using AudioStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AudioStore.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleSeeder = services.GetRequiredService<RoleSeeder>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            await DbInitializer.InitializeAsync(context, userManager, roleSeeder, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing the database");
            throw;
        }
    }
}
