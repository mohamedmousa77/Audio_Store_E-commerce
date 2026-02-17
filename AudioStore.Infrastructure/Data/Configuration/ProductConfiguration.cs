using AudioStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AudioStore.Infrastructure.Data.Configuration;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("ProductID")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200)
            .HasColumnType("nvarchar(200)");

        builder.Property(p => p.Brand)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(2000)
            .HasColumnType("nvarchar(2000)");

        builder.Property(p => p.Specifications)
            .HasColumnType("nvarchar(max)"); // JSON string

        builder.Property(p => p.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(p => p.StockQuantity)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(p => p.IsAvailable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.MainImage)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.GalleryImages)
            .HasColumnType("nvarchar(max)"); // JSON array

        builder.Property(p => p.IsNewProduct)
            .HasDefaultValue(false);

        builder.Property(p => p.IsFeatured)
            .HasDefaultValue(false);

        builder.Property(p => p.IsPublished)
            .HasDefaultValue(true);

        builder.Property(p => p.Slug)
            .IsRequired()
            .HasMaxLength(250)
            .HasColumnType("varchar(250)");

        // Timestamps da BaseEntity
        builder.Property(p => p.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(p => p.UpdatedAt)
            .IsRequired(false);

        builder.Property(p => p.IsDeleted)
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.CartItems)
            .WithOne(ci => ci.Product)
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.OrderItems)
            .WithOne(oi => oi.Product)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.IsPublished);
        builder.HasIndex(p => p.IsFeatured);
        builder.HasIndex(p => p.IsAvailable);
        builder.HasIndex(p => new { p.CategoryId, p.IsPublished, p.IsAvailable }); // Composite index per query comuni

        // Query Filters (Soft Delete)
        builder.HasQueryFilter(p => !p.IsDeleted);

    }

}
