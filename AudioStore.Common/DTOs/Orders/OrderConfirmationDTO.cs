namespace AudioStore.Common.DTOs.Orders;

public record OrderConfirmationDTO
{
    public string OrderNumber { get; init; } = string.Empty;
    public DateTime OrderDate { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Message { get; init; } = "Grazie per il tuo ordine!";
    public OrderDTO OrderDetails { get; init; } = null!;

}
