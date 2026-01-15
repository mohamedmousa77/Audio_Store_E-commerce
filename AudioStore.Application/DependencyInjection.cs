using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AudioStore.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // ✅ TODO: Aggiungi AutoMapper quando implementi i Profiles
        // services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // ✅ TODO: Aggiungi FluentValidation quando implementi i Validators
        // services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // ✅ TODO: Aggiungi Services quando li implementi
        // services.AddScoped<IAuthService, AuthService>();
        // services.AddScoped<IProductService, ProductService>();
        return services;
    }
}
