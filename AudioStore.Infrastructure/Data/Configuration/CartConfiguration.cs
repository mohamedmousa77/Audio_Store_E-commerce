using AudioStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AudioStore.Infrastructure.Data.Configuration;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("CartID")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.SessionId)
            .HasMaxLength(100)
            .HasColumnType("varchar(100)");

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Timestamps
        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(c => c.IsDeleted)
            .HasDefaultValue(false);

        // Computed Property (non mappato)
        builder.Ignore(c => c.TotalAmount);

        // Relationships
        builder.HasOne(c => c.User)
            .WithOne(u => u.Cart)
            .HasForeignKey<Cart>(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.CartItems)
            .WithOne(ci => ci.Cart)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(c => c.UserId).IsUnique();
        builder.HasIndex(c => c.SessionId);
        builder.HasIndex(c => c.IsActive);

        // Query Filter
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}