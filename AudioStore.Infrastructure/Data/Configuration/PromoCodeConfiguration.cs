using AudioStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AudioStore.Infrastructure.Data.Configuration;

public class PromoCodeConfiguration : IEntityTypeConfiguration<PromoCode>
{
    public void Configure(EntityTypeBuilder<PromoCode> builder)
    {
        builder.ToTable("PromoCodes");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("varchar(50)");

        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.DiscountValue)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.MinOrderAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.CurrentUsages)
            .HasDefaultValue(0);

        // Soft-delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
