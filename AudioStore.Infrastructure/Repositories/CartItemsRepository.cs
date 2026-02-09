using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AudioStore.Infrastructure.Repositories;

public class CartItemsRepository : Repository<CartItem>, ICartItemsRepository
{
    public CartItemsRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<CartItem?> GetCartItemWithProducts(int cartItemId)
    {
        return await _dbSet
            .Include(ci => ci.Cart)     // ✅ FIX: Include Cart navigation property
            .Include(ci => ci.Product)
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId);
    }
    public async Task<CartItem?> GetCartItemWithCart(int cartItemId)
    {
        return await _dbSet
            .Include(ci => ci.Cart)     // ✅ FIX: Include Cart navigation property
            .FirstOrDefaultAsync(ci => ci.Id == cartItemId);
    }
}
