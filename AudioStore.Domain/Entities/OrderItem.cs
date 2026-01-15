namespace AudioStore.Domain.Entities;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; } // Storicizzato
    public decimal Subtotal { get; set; } // Quantity * UnitPrice

    // Navigation Properties
    public virtual Order Order { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;

}
