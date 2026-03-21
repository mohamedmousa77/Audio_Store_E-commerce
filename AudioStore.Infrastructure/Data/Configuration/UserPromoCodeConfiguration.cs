using AudioStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AudioStore.Infrastructure.Data.Configuration;

public class UserPromoCodeConfiguration : IEntityTypeConfiguration<UserPromoCode>
{
    public void Configure(EntityTypeBuilder<UserPromoCode> builder)
    {
        builder.ToTable("UserPromoCodes");

        // Composite PK
        builder.HasKey(u => new { u.UserId, u.PromoCodeId });

        builder.Property(u => u.IsUsed)
            .HasDefaultValue(false);

        // FK → User
        builder.HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK → PromoCode
        builder.HasOne(u => u.PromoCode)
            .WithMany(p => p.UserPromoCodes)
            .HasForeignKey(u => u.PromoCodeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
