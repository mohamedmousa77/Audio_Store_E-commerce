// Configurazione Serilog PRIMA del builder
using Asp.Versioning;
using AudioStore.Application;
using AudioStore.Infrastructure;
using AudioStore.Infrastructure.Data;
using Microsoft.OpenApi;
using Serilog;
using System.Net.Sockets;
using System.Reflection;


Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/audiostore-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.Seq("http://localhost:5341") // Opzionale: per Seq dashboard
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .CreateLogger();

try
{
    Log.Information("Starting AudioStore API");

    var builder = WebApplication.CreateBuilder(args);

    // Usa Serilog come logger
    builder.Host.UseSerilog();

    // Determina ambiente(Development, Docker, Production)
    var environment = builder.Environment.EnvironmentName;
    Log.Information("Running in {Environment} mode", environment);


    // Add services to the container
    builder.Services.AddControllers();

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version")
        );
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Audio Store API",
            Version = "v1",
            Description = "E-commerce API per prodotti audio premium",
            Contact = new OpenApiContact
            {
                Name = "Audio Store Team",
                Email = "support@audiostore.com"
            }
        });

        // JWT Authentication in Swagger
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Inserisci il token JWT in questo formato: Bearer {token}"
        });

        //options.AddSecurityRequirement(new OpenApiSecurityRequirement
        //{
        //    {
        //        new OpenApiSecurityScheme
        //        {
        //            Reference = new OpenApiReferenceWithDescription
        //            {
        //                Type = ReferenceType.SecurityScheme,
        //                Id = "Bearer"
        //            }
        //        },
        //        Array.Empty<string>()
        //    }
        //});

        // XML Comments (opzionale)
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // AutoMapper (modern way - NO deprecated package)
    // builder.Services.AddAutoMapper(typeof(MappingProfile));

    // CORS per Angular
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

    // Infrastructure Services (DB, Identity, Repositories, ecc.)
    builder.Services.AddInfrastructure(builder.Configuration);

    // Application Services (Business Logic)
     builder.Services.AddApplication();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            // Verifica se il database esiste e applica migrations
            await context.Database.EnsureCreatedAsync(); // Solo per testing rapido
            // Per produzione usa: await context.Database.MigrateAsync();
            Log.Information("Database initialized successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while initializing the database");
        }
    }

    // Middleware Pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Audio Store API v1");
            options.RoutePrefix = string.Empty; // Swagger UI alla root
        });
    }

    app.UseSerilogRequestLogging(); // Log HTTP requests

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