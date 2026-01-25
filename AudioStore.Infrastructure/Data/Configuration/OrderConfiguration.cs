using AudioStore.Common.Enums;
using AudioStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AudioStore.Infrastructure.Data.Configuration
{
    internal class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);

            builder.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            builder.Property(o => o.OrderDate)
                .IsRequired();

            // ✅ UserId nullable per guest
            builder.Property(o => o.UserId)
                .IsRequired(false);

            // Customer Info
            builder.Property(o => o.CustomerFirstName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.CustomerLastName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.CustomerEmail)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(o => o.CustomerPhone)
                .IsRequired()
                .HasMaxLength(20);

            // Shipping Address
            builder.Property(o => o.ShippingStreet)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(o => o.ShippingCity)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(o => o.ShippingPostalCode)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(o => o.ShippingCountry)
                .IsRequired()
                .HasMaxLength(100);

            // Totals
            builder.Property(o => o.Subtotal)
                .HasColumnType("decimal(18,2)");

            builder.Property(o => o.ShippingCost)
                .HasColumnType("decimal(18,2)");

            builder.Property(o => o.Tax)
                .HasColumnType("decimal(18,2)");

            builder.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            // Payment & Status
            builder.Property(o => o.PaymentMethod)
                .HasMaxLength(50)
                .HasDefaultValue("Cash on Delivery");

            builder.Property(o => o.Status)
                .HasConversion<int>()
                .HasDefaultValue(OrderStatus.Pending);

            builder.Property(o => o.Notes)
                .HasMaxLength(500);

            // Relationships
            builder.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(o => o.OrderNumber).IsUnique();
            builder.HasIndex(o => o.UserId);
            builder.HasIndex(o => o.Status);
            builder.HasIndex(o => o.OrderDate);
            builder.HasIndex(o => o.CustomerEmail);

            // Query Filter
            builder.HasQueryFilter(o => !o.IsDeleted);

        }

    }
}
