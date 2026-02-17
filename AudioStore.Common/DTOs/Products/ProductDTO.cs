namespace AudioStore.Common.DTOs.Products;

public class ProductDTO
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public string MainImage { get; init; } = string.Empty;
    public List<string>? GalleryImages { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsFeatured { get; init; }
    public int CategoryId { get; init; } 
    public string CategoryName { get; init; } = string.Empty;
}
