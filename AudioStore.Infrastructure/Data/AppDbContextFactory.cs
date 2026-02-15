using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AudioStore.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
{
        // 1. Cerchiamo il file appsettings.json nel progetto API
        //var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../AudioStore.Api");
        var currentDir = Directory.GetCurrentDirectory();
        string basePath;
        if (currentDir.EndsWith("AudioStore.Infrastructure"))
        {
            // Eseguito dalla cartella Infrastructure
            basePath = Path.Combine(currentDir, "../AudioStore.Api");
        }
        else
        {
            // Eseguito dalla root (o altro) - assume struttura standard
            basePath = Path.Combine(currentDir, "AudioStore.Api");
        }

        var configuration = new ConfigurationBuilder()
        .SetBasePath(basePath)
        .AddJsonFile("appsettings.json")
        .Build();

    var builder = new DbContextOptionsBuilder<AppDbContext>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");

    builder.UseSqlServer(connectionString);

    return new AppDbContext(builder.Options);
}
}