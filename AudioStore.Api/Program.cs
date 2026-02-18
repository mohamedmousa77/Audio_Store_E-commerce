using Asp.Versioning;
using AudioStore.Api.Extensions;
using AudioStore.Api.Middleware;
using AudioStore.Application;
using AudioStore.Infrastructure;
using AudioStore.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Security.Claims;

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

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "AudioStore API",
            Version = "v1",
            Description = "Audio Store E-commerce API"
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
        options.AddPolicy("AllowAnonimos", policy =>
        {
            policy.WithOrigins("https://2r77q06d-4200")
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

    // Serve static files from wwwroot (product/category images)
    app.UseStaticFiles();

    // Ensure image directories exist
    var imagesRoot = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "images");
    Directory.CreateDirectory(Path.Combine(imagesRoot, "products"));
    Directory.CreateDirectory(Path.Combine(imagesRoot, "categories"));

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

// Make Program accessible to test project
public partial class Program { }