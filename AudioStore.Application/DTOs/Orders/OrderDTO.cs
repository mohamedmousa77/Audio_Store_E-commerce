using AudioStore.Domain.Enums;

namespace AudioStore.Application.DTOs.Orders;

public record OrderDTO
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public int? UserId { get; set; }
    public OrderStatus OrderStatus { get; set; }

    // Customer Info
    public string CustomerFirstName { get; set; } = string.Empty;
    public string CustomerLastName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    //Shipping Address
    public string ShippingStreet { get; init; } = string.Empty;
    public string ShippingCity { get; init; } = string.Empty;
    public string ShippingPostalCode { get; init; } = string.Empty;
    public string ShippingCountry { get; init; } = string.Empty;

    // Order Items 
    public List<OrderItemDTO> Items { get; set; } = new();

    // Totals 
    public decimal Subtotal { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal Tax { get; init; }
    public decimal TotalAmount { get; init; }

    // Payment
    public string PaymentMethod { get; init; } = "Cash on Delivery";
    public string? Notes { get; init; }
}
