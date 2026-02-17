namespace AudioStore.Domain.Entities;

public class Product : BaseEntity
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Specifications { get; set; } 
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string MainImage { get; set; } = string.Empty;
    public List<string>? GalleryImages { get; set; } 
    public bool IsNewProduct { get; set; } = false;
    public bool IsFeatured { get; set; } = false;
    public bool IsPublished { get; set; } = true;
    public string Slug { get; set; } = string.Empty;

    // Navigation Properties
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
