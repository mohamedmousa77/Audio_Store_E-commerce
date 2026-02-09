namespace AudioStore.Common.DTOs.Products;

public record ProductFilterDTO
{
    public string? SearchTerm { get; init; }
    public int? CategoryId { get; init; }
    public string? Brand { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public bool? IsFeatured { get; init; }
    public bool? IsAvailable { get; init; }
    public bool? IsNew { get; init; }
    public string? SortBy { get; init; } // "price_asc", "price_desc", "name", "newest"
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 12;

}
