using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using AudioStore.Infrastructure.Identity;
using AudioStore.Infrastructure.Repositories;
using AudioStore.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AudioStore.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext con SQL Server
        services.AddDbContext<AppDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });

            var environment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });


        // ✅ Identity Configuration (moved to Identity folder)
        services.AddIdentityConfiguration();


        // Security
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ✅ Repository Pattern 
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartItemsRepository, CartItemsRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();



        return services;

    }
}
