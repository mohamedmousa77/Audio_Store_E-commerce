using AudioStore.Application.DTOs.Products;
using AudioStore.Common.Result;

namespace AudioStore.Application.Services.Interfaces;

public interface IProductService
{
    // Queries
    Task<Result<ProductDTO>> GetByIdAsync(int id);
    Task<Result<PaginatedResult<ProductDTO>>> GetAllAsync(ProductFilterDTO filter);
    Task<Result<IEnumerable<ProductDTO>>> GetFeaturedAsync(int count = 10);
    Task<Result<IEnumerable<ProductDTO>>> GetByCategoryAsync(int categoryId);
    Task<Result<IEnumerable<ProductDTO>>> SearchAsync(string searchTerm);
    Task<Result<IEnumerable<string>>> GetBrandsAsync();

    // Commands
    Task<Result<ProductDTO>> CreateAsync(CreateProductDTO dto);
    Task<Result<ProductDTO>> UpdateAsync(UpdateProductDTO dto);
    Task<Result> DeleteAsync(int id);
    Task<Result> UpdateStockAsync(int id, int quantity);
}
