using AudioStore.Domain.Entities;
using AudioStore.Domain.Enums;
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

            builder.Property(o => o.Id)
                .HasColumnName("OrderID")
                .ValueGeneratedOnAdd();

            builder.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(50)
                .HasColumnType("varchar(50)");

            builder.Property(o => o.CustomerName)
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            builder.Property(o => o.CustomerEmail)
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            builder.Property(o => o.CustomerPhone)
                .HasMaxLength(20)
                .HasColumnType("nvarchar(20)");

            builder.Property(o => o.OrderDate)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(o => o.Status)
                .IsRequired()
                .HasConversion<string>() // Salva enum come stringa nel DB
                .HasMaxLength(50)
                .HasDefaultValue(OrderStatus.Processing);

            builder.Property(o => o.TotalAmount)
                .IsRequired()
                .HasColumnType("decimal(18,2)")
                .HasPrecision(18, 2);

            builder.Property(o => o.PaymentMethod)
                .IsRequired()
                .HasMaxLength(100)
                .HasDefaultValue("Cash on Delivery");

            // Timestamps
            builder.Property(o => o.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(o => o.IsDeleted)
                .HasDefaultValue(false);

            // Relationships
            builder.HasOne(o => o.User)
                .WithMany(u => u.Orders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.SetNull); // Non eliminare ordini se user viene eliminato

            builder.HasOne(o => o.ShippingAddress)
                .WithMany(a => a.Orders)
                .HasForeignKey(o => o.ShippingAddressId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(o => o.OrderItems)
                .WithOne(oi => oi.Order)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade); // Elimina items quando elimini l'ordine

            // Indexes
            builder.HasIndex(o => o.OrderNumber).IsUnique();
            builder.HasIndex(o => o.UserId);
            builder.HasIndex(o => o.Status);
            builder.HasIndex(o => o.OrderDate).IsDescending(); // Per query ordinate per data
            builder.HasIndex(o => new { o.UserId, o.OrderDate }); // Composite index

            // Query Filter
            builder.HasQueryFilter(o => !o.IsDeleted);
        }

    }
}
