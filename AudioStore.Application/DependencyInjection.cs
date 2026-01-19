using AudioStore.Application.Behaviors;
using AudioStore.Application.Mapping;
using AudioStore.Application.Services.Implementations;
using AudioStore.Application.Services.Interfaces;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace AudioStore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        // ✅ AutoMapper
        //services.AddAutoMapper(typeof(MappingProfile));
        //services.AddAutoMapper(Assembly.GetExecutingAssembly());
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(MappingProfile).Assembly);
        });

        // ✅ FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        // ✅ MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // ✅ Registra il ValidationBehavior come pipeline behavior
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // ✅ Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
