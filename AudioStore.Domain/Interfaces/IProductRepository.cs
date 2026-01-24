using AudioStore.Domain.Entities;

namespace AudioStore.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IEnumerable<string>> GetAllBrands();
    Task<IEnumerable<Product>> GetAllFilteredProducts(string searchTerm);
    Task<IEnumerable<Product>> GetFeaturedProducts(int count);
    Task<Product?> GetProductById(int id);
    Task<IEnumerable<Product>> GetProductsByCategory(int categoryId);
}
