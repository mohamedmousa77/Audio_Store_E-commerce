using AudioStore.Domain.Enums;

namespace AudioStore.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty; // Es: ORD-2026-00001
    public int? UserId { get; set; } // Nullable per Guest
    public string? CustomerName { get; set; } // Per Guest
    public string? CustomerEmail { get; set; } // Per Guest
    public string? CustomerPhone { get; set; } // Per Guest
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Processing;
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash on Delivery";
    public int ShippingAddressId { get; set; }

    // Navigation Properties
    public virtual User? User { get; set; }
    public virtual Address ShippingAddress { get; set; } = null!;
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
