using AudioStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AudioStore.Infrastructure.Data.Configuration;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("CategoryID")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(c => c.Description)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.Property(c => c.Slug)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");

        // Relationships
        builder.HasMany(c => c.Products)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict); // Non eliminare categoria se ha prodotti

        // Indexes
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.Name);

    }
}
