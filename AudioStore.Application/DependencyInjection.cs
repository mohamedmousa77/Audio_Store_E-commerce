using AudioStore.Application.Behaviors;
using AudioStore.Application.Mapping;
using AudioStore.Application.Services.Implementations;
using AudioStore.Application.Services.Interfaces;
using AudioStore.Domain;
using AudioStore.Domain.Interfaces;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text;

namespace AudioStore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var assembly = Assembly.GetExecutingAssembly();
        //  AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddMaps(typeof(MappingProfile).Assembly);
        });

        //  FluentValidation
        services.AddValidatorsFromAssembly(assembly);

        //  MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            //  Registra il ValidationBehavior come pipeline behavior
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        //  JWT Configuration
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>();
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));


        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings!.Secret)),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        //  Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
