using AudioStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AudioStore.Infrastructure.Data.Configuration;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("AddressID")
            .ValueGeneratedOnAdd();

        builder.Property(a => a.UserId)
            .IsRequired(false);

        builder.Property(a => a.Street)
            .IsRequired()
            .HasMaxLength(500)
            .HasColumnType("nvarchar(500)");

        builder.Property(a => a.City)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(a => a.PostalCode)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnType("varchar(20)");

        builder.Property(a => a.Country)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("nvarchar(100)");

        builder.Property(a => a.IsDefault)
            .HasDefaultValue(false);

        // Timestamps
        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.IsDeleted)
            .HasDefaultValue(false);

        // Relationships
        builder.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => new { a.UserId, a.IsDefault }); // Per recuperare indirizzo default

        // Query Filter
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}