using Asp.Versioning;
using AudioStore.Api.Extensions;
using AudioStore.Api.Middleware;
using AudioStore.Application;
using AudioStore.Infrastructure;
using AudioStore.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Text;

// ✅ Configure Serilog from appsettings.json
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .Build())
    .CreateLogger();

try
{
    Log.Information("Starting AudioStore API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Add services
    builder.Services.AddControllers();

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
            };
        });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // Configure Swagger for API versioning
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Audio Store API",
            Version = "v1",
            Description = "Audio Store E-commerce API - Version 1.0\n\nFeatures:\n- JWT Authentication with Refresh Tokens\n- Product Management\n- Category Management\n- Shopping Cart\n- Order Processing",
            Contact = new OpenApiContact
            {
                Name = "Audio Store Team",
                Email = "support@audiostore.com"
            }
        });

        // JWT Bearer Authentication
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter 'Bearer' [space] and then your valid token.\n\nExample: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
        });

    });

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngular", policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
    });

    //  Infrastructure & Application
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication(builder.Configuration);
    
    //  Cached Decorators
    builder.Services.AddCachedDecorators();


    var app = builder.Build();

    //  Initialize Database with Seeding
    if (app.Environment.IsDevelopment())
    {
    // ============ DATABASE INITIALIZATION ============
    await app.Services.InitializeDatabaseAsync();
    }

    // Middleware
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("AllowAngular");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}