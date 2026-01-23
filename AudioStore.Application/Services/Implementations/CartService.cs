using AudioStore.Application.DTOs.Cart;
using AudioStore.Application.Services.Interfaces;
using AudioStore.Common.Constants;
using AudioStore.Common.Result;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AudioStore.Application.Services.Implementations;

internal class CartService : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CartService> _logger;

    public CartService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<CartService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }
    public async Task<Result<CartDTO>> GetOrCreateCartAsync(int? userId, string? sessionId)
    {
        try
        {
            if (userId == null && string.IsNullOrEmpty(sessionId))
            {
                return Result.Failure<CartDTO>("Devi fornire UserId o SessionId",
                    ErrorCode.BadRequest);
            }

            Cart? cart = null;

            if (userId.HasValue)
            {
                cart = await _unitOfWork.Carts
                    .Query()
                    .Include(c => c.CartItems)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId.Value);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cart = await _unitOfWork.Carts
                    .Query()
                    .Include(c => c.CartItems)
                    .ThenInclude(i => i.Product)
                    .FirstOrDefaultAsync(c => c.SessionId == sessionId);
            }

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    SessionId = sessionId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Carts.AddAsync(cart);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "New cart created - UserId: {UserId}, SessionId: {SessionId}",
                    userId,
                    sessionId);
            }

            var cartDto = _mapper.Map<CartDTO>(cart);
            return Result.Success(cartDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating cart");
            return Result.Failure<CartDTO>(
                "Errore recupero carrello",
                ErrorCode.InternalServerError);
        }

    }
    public async Task<Result<CartDTO>> AddItemAsync(AddToCartDTO dto)
    {
        try
        {
            if(dto.UserId == null && string.IsNullOrWhiteSpace(dto.SessionId))
            {
                return Result.Failure<CartDTO>("Devi fornire UserId o SessionId",
                    ErrorCode.BadRequest);
            }

            var cartResult = await GetOrCreateCartAsync(dto.UserId, dto.SessionId);
            if (cartResult.IsFailure)
                return cartResult;

            var cart = await GetCartEntityAsync(dto.UserId, dto.SessionId);
            if (cart == null)
            {
                return Result.Failure<CartDTO>(
                    "Errore recupero carrello",
                    ErrorCode.CartNotFound);
            }
            // Verifica prodotto
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null)
            {
                return Result.Failure<CartDTO>(
                    "Prodotto non trovato",
                    ErrorCode.ProductNotFound);
            }

            if (!product.IsAvailable)
            {
                return Result.Failure<CartDTO>(
                    "Prodotto non disponibile",
                    ErrorCode.ProductNotAvailable);
            }

            // Verifica stock
            var existingItem = cart.CartItems.FirstOrDefault(i => i.ProductId == dto.ProductId);
            var totalQuantity = existingItem != null
                ? existingItem.Quantity + dto.Quantity
                : dto.Quantity;

            if (product.StockQuantity < totalQuantity)
            {
                return Result.Failure<CartDTO>(
                    $"Stock insufficiente. Disponibili: {product.StockQuantity}",
                    ErrorCode.InsufficientStock);
            }

            // Aggiorna o aggiungi item
            if (existingItem != null)
            {
                existingItem.Quantity = totalQuantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
                 _unitOfWork.CartItems.Update(existingItem);
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UnitPrice = product.Price,
                    CreatedAt = DateTime.UtcNow
                };
                await _unitOfWork.CartItems.AddAsync(cartItem);
            }

            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Product {ProductId} added to cart (UserId: {UserId}, SessionId: {SessionId})",
                dto.ProductId,
                dto.UserId,
                dto.SessionId);

            // Ricarica carrello aggiornato
            cart = await GetCartEntityAsync(dto.UserId, dto.SessionId);
            var cartDto = _mapper.Map<CartDTO>(cart!);

            return Result.Success(cartDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to cart");
            return Result.Failure<CartDTO>(
                "Errore aggiunta prodotto al carrello",
                ErrorCode.InternalServerError);
        }

    }    

    public async Task<Result<CartDTO>> MergeGuestCartToUserAsync(string sessionId, int userId)
    {
        try
        {
            // Trova carrello guest
            var guestCart = await _unitOfWork.Carts
                .Query()
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);

            if (guestCart == null || !guestCart.CartItems.Any())
            {
                // Nessun carrello guest da mergeare
                return await GetOrCreateCartAsync(userId, null);
            }

            // Trova o crea carrello utente
            var userCart = await _unitOfWork.Carts
                .Query()
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (userCart == null)
            {
                // Converte carrello guest in carrello utente
                guestCart.UserId = userId;
                guestCart.SessionId = null;
                guestCart.UpdatedAt = DateTime.UtcNow;

                 _unitOfWork.Carts.Update(guestCart);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Guest cart converted to user cart (SessionId: {SessionId}, UserId: {UserId})",
                    sessionId,
                    userId);

                var cartDto = _mapper.Map<CartDTO>(guestCart);
                return Result.Success(cartDto);
            }

            // Merge items da guest cart a user cart
            foreach (var guestItem in guestCart.CartItems)
            {
                var existingItem = userCart.CartItems
                    .FirstOrDefault(i => i.ProductId == guestItem.ProductId);

                if (existingItem != null)
                {
                    // Aggiorna quantità
                    existingItem.Quantity += guestItem.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                     _unitOfWork.CartItems.Update(existingItem);
                }
                else
                {
                    // Sposta item a user cart
                    guestItem.CartId = userCart.Id;
                     _unitOfWork.CartItems.Update(guestItem);
                }
            }

            // Elimina carrello guest
             _unitOfWork.Carts.Delete(guestCart);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Guest cart merged to user cart (SessionId: {SessionId}, UserId: {UserId})",
                sessionId,
                userId);

            // Ricarica carrello utente aggiornato
            userCart = await GetCartEntityAsync(userId, null);
            var userCartDto = _mapper.Map<CartDTO>(userCart!);

            return Result.Success(userCartDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error merging guest cart to user cart");
            return Result.Failure<CartDTO>(
                "Errore merge carrello",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<CartDTO>> RemoveItemAsync(int cartItemId)
    {
        try
        {
            var cartItem = await _unitOfWork.CartItems
                .Query()
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == cartItemId);

            if (cartItem == null)
            {
                return Result.Failure<CartDTO>(
                    "Elemento carrello non trovato",
                    ErrorCode.NotFound);
            }

            var userId = cartItem.Cart.UserId;
            var sessionId = cartItem.Cart.SessionId;

            _unitOfWork.CartItems.Update(cartItem);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Cart item {CartItemId} removed", cartItemId);

            // Ricarica carrello
            var cart = await GetCartEntityAsync(userId, sessionId);
            var cartDto = _mapper.Map<CartDTO>(cart!);

            return Result.Success(cartDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cart item {CartItemId}", cartItemId);
            return Result.Failure<CartDTO>(
                "Errore rimozione prodotto dal carrello",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result> ClearCartAsync(int? userId, string? sessionId)
    {
        try
        {
            var cart = await GetCartEntityAsync(userId, sessionId);
            if (cart == null)
                return Result.Success();

            _unitOfWork.CartItems.DeleteRange(cart.CartItems);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Cart cleared (UserId: {UserId}, SessionId: {SessionId})",
                userId,
                sessionId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing cart");
            return Result.Failure(
                "Errore svuotamento carrello",
                ErrorCode.InternalServerError);
        }
    }


    public async Task<Result<CartDTO>> UpdateItemQuantityAsync(UpdateCartItemDTO dto)
    {
        try
        {
            var cartItem = await _unitOfWork.CartItems
                .Query()
                .Include(ci => ci.CartId)
                .Include(ci => ci.Product)
                .FirstOrDefaultAsync(ci => ci.Id == dto.CartItemId);

            if(cartItem == null)
            {
                return Result.Failure<CartDTO>("Elemento carrello non trovato",
                    ErrorCode.NotFound);
            }
            if (dto.Quantity <= 0)
            {
                return Result.Failure<CartDTO>("Quantità non valida",
                    ErrorCode.InvalidQuantity);
            }
            if (dto.Quantity > cartItem.Product.StockQuantity)
            {
                return Result.Failure<CartDTO>($"Stock insufficiente. Disponibili: {cartItem.Product.StockQuantity}",
                    ErrorCode.InsufficientStock);
            }

            cartItem.Quantity = dto.Quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;

             _unitOfWork.CartItems.Update(cartItem);
            await _unitOfWork.SaveChangesAsync();

            // Ricarica carrello
            var cart = await GetCartEntityAsync(
                cartItem.Cart.UserId,
                cartItem.Cart.SessionId);

            var cartDto = _mapper.Map<CartDTO>(cart!);
            return Result.Success(cartDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cart item {CartItemId}", dto.CartItemId);
            return Result.Failure<CartDTO>(
                "Errore aggiornamento quantità",
                ErrorCode.InternalServerError);
        }

    }

    // ============ PRIVATE HELPERS ============

    private async Task<Cart?> GetCartEntityAsync(int? userId, string? sessionId)
    {
        if (userId.HasValue)
        {
            return await _unitOfWork.Carts
                .Query()
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            return await _unitOfWork.Carts
                .Query()
                .Include(c => c.CartItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.SessionId == sessionId);
        }

        return null;
    }

}
