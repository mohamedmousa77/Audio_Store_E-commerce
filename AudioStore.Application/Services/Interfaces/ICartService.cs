using AudioStore.Application.DTOs.Cart;
using AudioStore.Common.Result;

namespace AudioStore.Application.Services.Interfaces;

public interface ICartService
{
    Task<Result<CartDTO>> GetOrCreateCartAsync(int? userId, string? sessionId);
    Task<Result<CartDTO>> AddItemAsync(AddToCartDTO dto);
    Task<Result<CartDTO>> UpdateItemQuantityAsync(UpdateCartItemDTO dto);
    Task<Result<CartDTO>> RemoveItemAsync(int cartItemId);
    Task<Result> ClearCartAsync(int? userId, string? sessionId);

    // In order to Merge guest cart to user cart dopo login
    Task<Result<CartDTO>> MergeGuestCartToUserAsync(string sessionId, int userId);

}
