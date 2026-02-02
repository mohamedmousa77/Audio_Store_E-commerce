using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Cart;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AudioStore.Application.Services.Implementations;

public class CartService : ICartService
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
                cart = await _unitOfWork.Carts.GetCartByUserId(userId.Value);
            }
            else if (!string.IsNullOrEmpty(sessionId))
            {
                cart = await _unitOfWork.Carts.GetCartBySessionId(sessionId);
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
            if (dto.UserId == null && string.IsNullOrWhiteSpace(dto.SessionId))
            {
                return Result.Failure<CartDTO>("Devi fornire UserId o SessionId",
                    ErrorCode.BadRequest);
            }

            var cart = await GetCartEntityAsync(dto.UserId, dto.SessionId);
            if (cart == null)
            {
                return Result.Failure<CartDTO>(
                    "Errore recupero carrello", ErrorCode.CartNotFound);
            }
            // Verifica prodotto
            var product = await _unitOfWork.Products.GetByIdAsync(dto.ProductId);
            if (product == null)
            {
                return Result.Failure<CartDTO>(
                    "Prodotto non trovato", ErrorCode.ProductNotFound);
            }

            if (!product.IsAvailable)
            {
                return Result.Failure<CartDTO>(
                    "Prodotto non disponibile", ErrorCode.ProductNotAvailable);
            }

            // Verifica item in carrello or not.
            var existingItem = cart.CartItems.FirstOrDefault(i => i.ProductId == dto.ProductId);
            var totalQuantity = existingItem != null
                ? existingItem.Quantity + dto.Quantity
                : dto.Quantity;

            // verifica stock
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
            // ============ STEP 1: VALIDAZIONE INPUT ============
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return Result.Failure<CartDTO>(
                    "SessionId richiesto",
                    ErrorCode.BadRequest);
            }

            if (userId <= 0)
            {
                return Result.Failure<CartDTO>(
                    "UserId non valido",
                    ErrorCode.BadRequest);
            }

            // Inizia transazione
            await _unitOfWork.BeginTransactionAsync();

            // ============ STEP 2: CARICA CARRELLI CON INCLUDES ============
            // Carica guest cart con items e products in una query
            var guestCart = await _unitOfWork.Carts.GetCartBySessionId(sessionId);

            // Se non c'è guest cart o è vuoto, ritorna il cart dell'utente
            if (guestCart == null || !guestCart.CartItems.Any())
            {
                _logger.LogInformation(
                    "No guest cart to merge for SessionId: {SessionId}",
                    sessionId);

                await _unitOfWork.CommitTransactionAsync();
                return await GetOrCreateCartAsync(userId, null);
            }

            // Carica user cart con items in una query
            var userCart = await _unitOfWork.Carts.GetCartByUserId(userId);

            // ============ STEP 3: SCENARIO 1 - USER NON HA CART ============
            if (userCart == null)
            {
                // Converti semplicemente il guest cart in user cart
                guestCart.UserId = userId;
                guestCart.SessionId = null;
                guestCart.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Carts.Update(guestCart);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation(
                    "Guest cart converted to user cart (SessionId: {SessionId}, UserId: {UserId})",
                    sessionId, userId);

                var convertedCartDto = _mapper.Map<CartDTO>(guestCart);
                return Result.Success(convertedCartDto);
            }

            // ============ STEP 4: SCENARIO 2 - MERGE DUE CARRELLI ============

            // Prepara liste per batch operations
            var itemsToUpdate = new List<CartItem>();
            var stockValidationErrors = new List<string>();

            foreach (var guestItem in guestCart.CartItems)
            {
                var existingUserItem = userCart.CartItems
                    .FirstOrDefault(ui => ui.ProductId == guestItem.ProductId);

                if (existingUserItem != null)
                {
                    // ============ MERGE: Somma quantità ============
                    var newQuantity = existingUserItem.Quantity + guestItem.Quantity;

                    // Validazione stock
                    if (guestItem.Product.StockQuantity < newQuantity)
                    {
                        stockValidationErrors.Add(
                            $"{guestItem.Product.Name}: richiesti {newQuantity}, disponibili {guestItem.Product.StockQuantity}");

                        // Usa la quantità massima disponibile
                        newQuantity = guestItem.Product.StockQuantity;
                    }

                    existingUserItem.Quantity = newQuantity;
                    existingUserItem.UnitPrice = guestItem.UnitPrice; // Aggiorna prezzo se cambiato
                    existingUserItem.UpdatedAt = DateTime.UtcNow;

                    itemsToUpdate.Add(existingUserItem);
                }
                else
                {
                    // ============ NUOVO ITEM: Sposta a user cart ============

                    // Validazione stock
                    if (guestItem.Product.StockQuantity < guestItem.Quantity)
                    {
                        stockValidationErrors.Add(
                            $"{guestItem.Product.Name}: richiesti {guestItem.Quantity}, disponibili {guestItem.Product.StockQuantity}");

                        guestItem.Quantity = guestItem.Product.StockQuantity;
                    }

                    guestItem.CartId = userCart.Id;
                    guestItem.UpdatedAt = DateTime.UtcNow;

                    itemsToUpdate.Add(guestItem);
                }
            }

            // ============ STEP 5: BATCH UPDATE ============
            if (itemsToUpdate.Any())
            {
                _unitOfWork.CartItems.UpdateRange(itemsToUpdate);
            }

            // ============ STEP 6: ELIMINA GUEST CART ============
            _unitOfWork.Carts.Delete(guestCart);

            // ============ STEP 7: SALVA TUTTO IN UNA TRANSAZIONE ============
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation(
                "Guest cart merged to user cart - SessionId: {SessionId}, UserId: {UserId}, Items merged: {ItemCount}",
                sessionId, userId, guestCart.CartItems.Count);

            // Log warning se ci sono stati problemi di stock
            if (stockValidationErrors.Any())
            {
                _logger.LogWarning(
                    "Stock validation issues during merge: {Errors}",
                    string.Join("; ", stockValidationErrors));
            }

            // ============ STEP 8: RICARICA USER CART AGGIORNATO ============
            var finalUserCart = await _unitOfWork.Carts.GetCartByUserId(userId);

            var resultDto = _mapper.Map<CartDTO>(finalUserCart!);

            // Aggiungi warnings se necessario
            if (stockValidationErrors.Any())
            {
                // Nota: Potresti voler estendere Result per supportare warnings
                _logger.LogInformation("Merge completed with stock warnings");
            }

            return Result.Success(resultDto);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();

            _logger.LogError(ex,
                "Error merging guest cart to user cart - SessionId: {SessionId}, UserId: {UserId}",
                sessionId, userId);

            return Result.Failure<CartDTO>(
                "Errore durante l'unione del carrello",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<CartDTO>> RemoveItemAsync(int cartItemId)
    {
        try
        {
            var cartItem = await _unitOfWork.CartItems.GetCartItemWithCart(cartItemId);

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
            var cartItem = await _unitOfWork.CartItems.GetCartItemWithProducts(dto.CartItemId);

            if (cartItem == null)
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
            return await _unitOfWork.Carts.GetCartByUserId(userId.Value);
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            return await _unitOfWork.Carts.GetCartBySessionId(sessionId);
        }

        return null;
    }

}
