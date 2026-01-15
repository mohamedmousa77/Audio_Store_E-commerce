namespace AudioStore.Domain.Entities;

public class CartItem : BaseEntity
{
    public int CartId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; } // Storicizzato al momento dell'aggiunta

    // Navigation Properties
    public virtual Cart Cart { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;

    // Computed Property
    public decimal Subtotal => Quantity * UnitPrice;
}
