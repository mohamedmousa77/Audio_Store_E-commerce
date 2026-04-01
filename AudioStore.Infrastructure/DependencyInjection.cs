using AudioStore.Common.Configuration;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.BackgroundJobs;
using AudioStore.Infrastructure.Cashing.Extensions;
using AudioStore.Infrastructure.Data;
using AudioStore.Infrastructure.Email;
using AudioStore.Infrastructure.Identity;
using AudioStore.Infrastructure.Repositories;
using AudioStore.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


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

                // ⚠️ TEMPORARILY DISABLED: EnableRetryOnFailure conflicts with manual transactions
                // Re-enable this later with proper execution strategy wrapping
                // sqlOptions.EnableRetryOnFailure(
                //     maxRetryCount: 5,
                //     maxRetryDelay: TimeSpan.FromSeconds(30),
                //     errorNumbersToAdd: null);
            });

            var environment = configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT");
            if (environment == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });


        // Identity Configuration (moved to Identity folder)
        services.AddIdentityConfiguration();

        // Caching Configuration (Redis or Memory cache)
        services.AddCaching(configuration);

        // Security
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // DirectIQ 
        // 1. Bind 
        services.Configure<DirectIqSettings>(configuration.GetSection(DirectIqSettings.SectionName));

        // 2. Named HttpClient
        services.AddHttpClient("DirectIQ", (sp, client) =>
        {
            var settings = sp.GetRequiredService<IOptions<DirectIqSettings>>().Value;
            client.BaseAddress = new Uri(settings.ApiUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/*+json");
            client.Timeout = TimeSpan.FromSeconds(30);

        });

        // Repository Pattern 
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICartItemsRepository, CartItemsRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IPromoCodeRepository, PromoCodeRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<IEmailService, EmailService>();
        services.AddHostedService<AbandonedCartEmailJob>();

        return services;

    }

    /// <summary>
    /// Add cached decorators for services
    /// Call this AFTER AddApplication() to wrap services with caching
    /// </summary>
    public static IServiceCollection AddCachedDecorators(this IServiceCollection services)
    {
        return services.AddCachedServices();
    }
}
