using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AudioStore.Infrastructure.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
{
        // 1. Cerchiamo il file appsettings.json nel progetto API
        var currentDir = Directory.GetCurrentDirectory();
        string basePath;

        // Verifica dove ci troviamo e imposta il basePath correttamente
        if (File.Exists(Path.Combine(currentDir, "appsettings.json")))
        {
            // Siamo già nella cartella dell'API
            basePath = currentDir;
        }
        else if (currentDir.EndsWith("AudioStore.Infrastructure"))
        {
            // Siamo nella cartella Infrastructure
            basePath = Path.Combine(currentDir, "..", "AudioStore.Api");
        }
        else if (Directory.Exists(Path.Combine(currentDir, "AudioStore.Api")))
        {
            // Siamo nella root
            basePath = Path.Combine(currentDir, "AudioStore.Api");
        }
        else
        {
            // Fallback: prova a usare la directory corrente
            basePath = currentDir;
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