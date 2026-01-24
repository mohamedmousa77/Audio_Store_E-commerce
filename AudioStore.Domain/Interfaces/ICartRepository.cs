using AudioStore.Domain.Entities;

namespace AudioStore.Domain.Interfaces;

public interface ICartRepository : IRepository<Cart>
{
    Task<Cart?> GetCartBySessionId(string sessionId);
    Task<Cart?> GetCartByUserId(int userId);
}
