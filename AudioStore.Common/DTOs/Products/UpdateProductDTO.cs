namespace AudioStore.Common.DTOs.Products;

public record UpdateProductDTO
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public string MainImage { get; init; } = string.Empty;
    public List<string>? GalleryImages { get; init; }
    public int CategoryId { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsAvailable { get; init; }
    public string? Specifications { get; init; }
    public decimal? Weight { get; init; }
    public string? Dimensions { get; init; }
    public string? WarrantyInfo { get; init; }

}
