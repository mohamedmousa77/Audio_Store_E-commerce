namespace AudioStore.Domain.Entities;

public class Cart : BaseEntity
{
    public int? UserId { get; set; } // Nullable per utenti non autenticati
    public string? SessionId { get; set; } // Per Guest users
    public bool IsActive { get; set; } = true;

    // Navigation Properties
    public virtual User? User { get; set; }
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    // Computed Property
    public bool IsGuestCart => UserId == null && !string.IsNullOrEmpty(SessionId);
    public bool IsUserCart => UserId.HasValue;
    public decimal TotalAmount => CartItems.Sum(item => item.Subtotal);
}
