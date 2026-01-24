using AudioStore.Domain.Entities;

namespace AudioStore.Domain.Interfaces;

public interface ICartItemsRepository : IRepository<CartItem>
{
    Task<CartItem?> GetCartItemWithCart(int cartItemId);
    Task<CartItem?> GetCartItemWithProducts(int cartItemId);
}
