namespace AudioStore.Common.DTOs.Orders;

public record CreateOrderDTO
{
    // Can be null for guest users
    public int? UserId { get; init; }

    //  Customer Info (obbligatori per guest, opzionali per utenti registrati)
    public string? CustomerFirstName { get; init; }
    public string? CustomerLastName { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerPhone { get; init; }

    // Shipping Address (sempre obbligatorio)
    public string ShippingStreet { get; init; } = string.Empty;
    public string ShippingCity { get; init; } = string.Empty;
    public string ShippingPostalCode { get; init; } = string.Empty;
    public string ShippingCountry { get; init; } = string.Empty;

    // ✅ Order Items (dal carrello)
    public List<CreateOrderItemDTO> Items { get; init; } = new();

    // ✅ Notes opzionali
    public string? Notes { get; init; }
}
