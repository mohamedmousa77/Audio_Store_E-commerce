namespace AudioStore.Domain.Entities;

public class Category
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string Slug { get; set; } = string.Empty; // URL-friendly

    // Navigation Properties
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

}
