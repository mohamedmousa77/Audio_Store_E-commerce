using AudioStore.Application.Services.Implementations;
using AudioStore.Application.Services.Interfaces;
using AudioStore.Infrastructure.Cashing.Configuration;
using AudioStore.Infrastructure.Cashing.Decorators;
using AudioStore.Infrastructure.Cashing.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace AudioStore.Infrastructure.Cashing.Extensions;

/// <summary>
/// Extension methods for registering cached service decorators
/// </summary>
public static class CachedServicesExtensions
{
    /// <summary>
    /// Add cached decorators for services
    /// Call this AFTER AddApplication() to decorate existing service registrations
    /// </summary>
    public static IServiceCollection AddCachedServices(this IServiceCollection services)
    {
        // Decorate IProductService with caching
        services.Decorate<IProductService>((inner, sp) =>
        {
            var cache = sp.GetRequiredService<ICacheService>();
            var config = sp.GetRequiredService<CacheConfiguration>();
            var logger = sp.GetRequiredService<ILogger<CachedProductService>>();
            return new CachedProductService(inner, cache, config, logger);
        });

        // Decorate ICategoryService with caching
        services.Decorate<ICategoryService>((inner, sp) =>
        {
            var cache = sp.GetRequiredService<ICacheService>();
            var config = sp.GetRequiredService<CacheConfiguration>();
            var logger = sp.GetRequiredService<ILogger<CachedCategoryService>>();
            return new CachedCategoryService(inner, cache, config, logger);
        });

        // Decorate IDashboardService with caching
        services.Decorate<IDashboardService>((inner, sp) =>
        {
            var cache = sp.GetRequiredService<ICacheService>();
            var config = sp.GetRequiredService<CacheConfiguration>();
            var logger = sp.GetRequiredService<ILogger<CachedDashboardService>>();
            return new CachedDashboardService(inner, cache, config, logger);
        });

        return services;
    }
}

/// <summary>
/// Extension method for service decoration (Scrutor-like pattern)
/// </summary>
public static class ServiceCollectionDecoratorExtensions
{
    public static IServiceCollection Decorate<TInterface>(
        this IServiceCollection services,
        Func<TInterface, IServiceProvider, TInterface> decorator)
        where TInterface : class
    {
        // Find the service descriptor
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TInterface));
        
        if (descriptor == null)
        {
            throw new InvalidOperationException(
                $"Service of type {typeof(TInterface).Name} is not registered.");
        }

        // Remove the original registration
        services.Remove(descriptor);

        // Create a new descriptor that wraps the original
        services.Add(ServiceDescriptor.Describe(
            typeof(TInterface),
            sp =>
            {
                // Get the original service
                TInterface inner;
                
                if (descriptor.ImplementationInstance != null)
                {
                    inner = (TInterface)descriptor.ImplementationInstance;
                }
                else if (descriptor.ImplementationFactory != null)
                {
                    inner = (TInterface)descriptor.ImplementationFactory(sp);
                }
                else if (descriptor.ImplementationType != null)
                {
                    inner = (TInterface)ActivatorUtilities.CreateInstance(sp, descriptor.ImplementationType);
                }
                else
                {
                    throw new InvalidOperationException("Invalid service descriptor");
                }

                // Apply the decorator
                return decorator(inner, sp);
            },
            descriptor.Lifetime));

        return services;
    }
}
