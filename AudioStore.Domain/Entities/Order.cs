using AudioStore.Common.Enums;

namespace AudioStore.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    //  Nullable per guest users
    public int? UserId { get; set; }

    //  Customer Info (per guest checkout)
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    //  Shipping Address
    public string ShippingStreet { get; set; } = string.Empty;
    public string ShippingCity { get; set; } = string.Empty;
    public string ShippingPostalCode { get; set; } = string.Empty;
    public string ShippingCountry { get; set; } = string.Empty;

    //  Totals
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Tax { get; set; }
    public decimal TotalAmount { get; set; }

    //  Payment & Status
    public string PaymentMethod { get; set; } = "Cash on Delivery";
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public string? Notes { get; set; }

    // Navigation
    public virtual User? User { get; set; }
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

}
