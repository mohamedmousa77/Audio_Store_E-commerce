using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AudioStore.Infrastructure.Repositories;

public class ProductRepository : Repository<Product>, IProductRepository
{

    public ProductRepository(AppDbContext context) : base(context)
    {
    }
    public async Task<Product?> GetProductById( int id)
    {
        return await _dbSet
            .Where(p => !p.IsDeleted)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
    public async Task<IEnumerable<Product>> GetFeaturedProducts(int count)
    {
        return await _dbSet
            .Where(p => p.IsFeatured && p.IsAvailable && !p.IsDeleted)
            .Include(p => p.Category)            
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetProductsByCategory(int categoryId)
    {
        return await _dbSet
            .Where(p => !p.IsDeleted && categoryId == p.CategoryId )
            .Include(p => p.Category)            
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetAllFilteredProducts(string searchTerm)
    {
        return await _dbSet             
                .Where(p =>
                    !p.IsDeleted &&
                    p.Name.ToLower().Contains(searchTerm) ||
                    p.Brand.ToLower().Contains(searchTerm) ||
                    p.Description.ToLower().Contains(searchTerm))
                .Include(p => p.Category)
                .Take(20)
                .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetAllBrands()
    {
        return await _dbSet
            .Where(p => !p.IsDeleted)
            .AsNoTracking()
            .Select(p => p.Brand)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync();
    }
}
