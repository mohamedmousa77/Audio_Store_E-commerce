using AudioStore.Domain.Entities;
using AudioStore.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AudioStore.Infrastructure.Data;

public class AppDbContext : IdentityDbContext<User, ApplicationRole, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // DbSets
    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Cart> Carts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Applica tutte le configurazioni
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Configurazione per salvare List<string> come JSON nel DB
        // Gestisce anche i dati legacy (stringhe semplici, non JSON)
        modelBuilder.Entity<Product>()
            .Property(p => p.GalleryImages)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions)null!),
                v => ConvertGalleryImages(v)
            );

        // Personalizza nomi tabelle Identity (opzionale)
        modelBuilder.Entity<User>().ToTable("Users");
        modelBuilder.Entity<ApplicationRole>().ToTable("Roles");
        modelBuilder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e =>
            e.Entity is BaseEntity
            && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (BaseEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }

            if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Converte il valore GalleryImages dal DB a List&lt;string&gt;,
    /// gestendo sia dati JSON validi che stringhe semplici (dati legacy)
    /// </summary>
    private static List<string>? ConvertGalleryImages(string? dbValue)
    {
        if (string.IsNullOrWhiteSpace(dbValue))
            return new List<string>();

        // Se inizia con '[', è un array JSON valido
        if (dbValue.TrimStart().StartsWith("["))
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(dbValue,
                    (System.Text.Json.JsonSerializerOptions)null!) ?? new List<string>();
            }
            catch
            {
                // Se il JSON è malformato, tratta come stringa semplice
                return new List<string> { dbValue };
            }
        }

        // Dato legacy: stringa semplice (es. "SomeImage.jpg") → la avvolgiamo in una lista
        return new List<string> { dbValue };
    }
}
