using AudioStore.Application.Services.Implementations;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Cart;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Tests.Helpers;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioStore.Tests.UnitTests.Services;

/// <summary>
/// Unit tests for CartService
/// Tests business logic for cart operations including add/remove items,
/// quantity updates, and guest cart merging
/// </summary>
public class CartServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<CartService>> _loggerMock;
    private readonly Mock<ICartRepository> _cartRepositoryMock;
    private readonly Mock<ICartItemsRepository> _cartItemRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly CartService _cartService;

    public CartServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<CartService>>();
        _cartRepositoryMock = new Mock<ICartRepository>();
        _cartItemRepositoryMock = new Mock<ICartItemsRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();

        _unitOfWorkMock.Setup(x => x.Carts).Returns(_cartRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.CartItems).Returns(_cartItemRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Products).Returns(_productRepositoryMock.Object);

        // Setup transaction mocks
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        _cartService = new CartService(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    #region AddItemAsync Tests

    [Fact]
    public async Task AddItemAsync_WithNewProduct_AddsItemToCart()
    {
        // Arrange
        var userId = 1;
        var product = TestDataBuilder.Product()
            .WithId(1)
            .WithPrice(99.99m)
            .WithStockQuantity(50)
            .Build();

        var cart = new Cart
        {
            Id = 1,
            UserId = userId,
            CartItems = new List<CartItem>()
        };

        var addToCartDto = new AddToCartDTO
        {
            UserId = userId,
            ProductId = 1,
            Quantity = 2
        };

        _cartRepositoryMock.Setup(x => x.GetCartByUserId(userId))
            .ReturnsAsync(cart);

        _productRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(product);

        _cartItemRepositoryMock.Setup(x => x.AddAsync(It.IsAny<CartItem>()))
            .ReturnsAsync((CartItem ci) => ci);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var cartDto = new CartDTO { Id = 1 };
        _mapperMock.Setup(x => x.Map<CartDTO>(It.IsAny<Cart>()))
            .Returns(cartDto);

        // Act
        var result = await _cartService.AddItemAsync(addToCartDto);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();

        _cartItemRepositoryMock.Verify(x => x.AddAsync(It.Is<CartItem>(ci =>
            ci.CartId == 1 &&
            ci.ProductId == 1 &&
            ci.Quantity == 2 &&
            ci.UnitPrice == 99.99m)), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_WithExistingProduct_UpdatesQuantity()
    {
        // Arrange
        var userId = 1;
        var product = TestDataBuilder.Product()
            .WithId(1)
            .WithStockQuantity(50)
            .Build();

        var existingCartItem = TestDataBuilder.CartItem()
            .WithCartId(1)
            .WithProductId(1)
            .WithQuantity(3) // Already 3 in cart
            .Build();

        var cart = new Cart
        {
            Id = 1,
            UserId = userId,
            CartItems = new List<CartItem> { existingCartItem }
        };

        var addToCartDto = new AddToCartDTO
        {
            UserId = userId,
            ProductId = 1,
            Quantity = 2 // Adding 2 more
        };

        _cartRepositoryMock.Setup(x => x.GetCartByUserId(userId))
            .ReturnsAsync(cart);

        _productRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(product);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var cartDto = new CartDTO { Id = 1 };
        _mapperMock.Setup(x => x.Map<CartDTO>(It.IsAny<Cart>()))
            .Returns(cartDto);

        // Act
        var result = await _cartService.AddItemAsync(addToCartDto);

        // Assert
        result.Should().BeSuccess();

        _cartItemRepositoryMock.Verify(x => x.Update(It.Is<CartItem>(ci =>
            ci.ProductId == 1 &&
            ci.Quantity == 5)), Times.Once); // 3 + 2 = 5
    }

    [Fact]
    public async Task AddItemAsync_WithInsufficientStock_ReturnsError()
    {
        // Arrange
        var userId = 1;
        var product = TestDataBuilder.Product()
            .WithId(1)
            .WithStockQuantity(5) // Only 5 available
            .Build();

        var cart = new Cart
        {
            Id = 1,
            UserId = userId,
            CartItems = new List<CartItem>()
        };

        var addToCartDto = new AddToCartDTO
        {
            UserId = userId,
            ProductId = 1,
            Quantity = 10 // Requesting 10, but only 5 available
        };

        _cartRepositoryMock.Setup(x => x.GetCartByUserId(userId))
            .ReturnsAsync(cart);

        _productRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(product);

        // Act
        var result = await _cartService.AddItemAsync(addToCartDto);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.InsufficientStock);

        _cartItemRepositoryMock.Verify(x => x.AddAsync(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task AddItemAsync_WithNonExistentProduct_ReturnsError()
    {
        // Arrange
        var userId = 1;
        var cart = new Cart { Id = 1, UserId = userId, CartItems = new List<CartItem>() };

        var addToCartDto = new AddToCartDTO
        {
            UserId = userId,
            ProductId = 999, // Non-existent
            Quantity = 1
        };

        _cartRepositoryMock.Setup(x => x.GetCartByUserId(userId))
            .ReturnsAsync(cart);

        _productRepositoryMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _cartService.AddItemAsync(addToCartDto);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.ProductNotFound);
    }

    [Fact]
    public async Task AddItemAsync_WithUnavailableProduct_ReturnsError()
    {
        // Arrange
        var userId = 1;
        var product = TestDataBuilder.Product()
            .WithId(1)
            .Build();
        product.IsAvailable = false; // Product not available

        var cart = new Cart { Id = 1, UserId = userId, CartItems = new List<CartItem>() };

        var addToCartDto = new AddToCartDTO
        {
            UserId = userId,
            ProductId = 1,
            Quantity = 1
        };

        _cartRepositoryMock.Setup(x => x.GetCartByUserId(userId))
            .ReturnsAsync(cart);

        _productRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(product);

        // Act
        var result = await _cartService.AddItemAsync(addToCartDto);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.ProductNotAvailable);
    }

    #endregion

    #region UpdateItemQuantityAsync Tests

    [Fact]
    public async Task UpdateItemQuantityAsync_WithValidQuantity_UpdatesItem()
    {
        // Arrange
        var product = TestDataBuilder.Product()
            .WithId(1)
            .WithStockQuantity(50)
            .Build();

        var cartItem = TestDataBuilder.CartItem()
            .WithId(1)
            .WithProductId(1)
            .WithQuantity(5)
            .Build();
        cartItem.Product = product;
        cartItem.Cart = new Cart { Id = 1, UserId = 1 };

        var updateDto = new UpdateCartItemDTO
        {
            CartItemId = 1,
            Quantity = 10 // Update to 10
        };

        _cartItemRepositoryMock.Setup(x => x.GetCartItemWithProducts(1))
            .ReturnsAsync(cartItem);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var cartDto = new CartDTO { Id = 1 };
        _mapperMock.Setup(x => x.Map<CartDTO>(It.IsAny<Cart>()))
            .Returns(cartDto);

        // Act
        var result = await _cartService.UpdateItemQuantityAsync(updateDto);

        // Assert
        result.Should().BeSuccess();

        _cartItemRepositoryMock.Verify(x => x.Update(It.Is<CartItem>(ci =>
            ci.Id == 1 &&
            ci.Quantity == 10)), Times.Once);
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_WithInsufficientStock_ReturnsError()
    {
        // Arrange
        var product = TestDataBuilder.Product()
            .WithId(1)
            .WithStockQuantity(5) // Only 5 available
            .Build();

        var cartItem = TestDataBuilder.CartItem()
            .WithId(1)
            .WithProductId(1)
            .WithQuantity(2)
            .Build();
        cartItem.Product = product;
        cartItem.Cart = new Cart { Id = 1 };

        var updateDto = new UpdateCartItemDTO
        {
            CartItemId = 1,
            Quantity = 10 // Requesting 10, but only 5 available
        };

        _cartItemRepositoryMock.Setup(x => x.GetCartItemWithProducts(1))
            .ReturnsAsync(cartItem);

        // Act
        var result = await _cartService.UpdateItemQuantityAsync(updateDto);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.InsufficientStock);

        _cartItemRepositoryMock.Verify(x => x.Update(It.IsAny<CartItem>()), Times.Never);
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_WithZeroQuantity_ReturnsError()
    {
        // Arrange
        var updateDto = new UpdateCartItemDTO
        {
            CartItemId = 1,
            Quantity = 0 // Invalid quantity
        };

        var cartItem = TestDataBuilder.CartItem()
            .WithId(1)
            .Build();
        cartItem.Product = TestDataBuilder.Product().Build();
        cartItem.Cart = new Cart { Id = 1 };

        _cartItemRepositoryMock.Setup(x => x.GetCartItemWithProducts(1))
            .ReturnsAsync(cartItem);

        // Act
        var result = await _cartService.UpdateItemQuantityAsync(updateDto);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.InvalidQuantity);
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_WithNonExistentItem_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateCartItemDTO
        {
            CartItemId = 999,
            Quantity = 5
        };

        _cartItemRepositoryMock.Setup(x => x.GetCartItemWithProducts(999))
            .ReturnsAsync((CartItem?)null);

        // Act
        var result = await _cartService.UpdateItemQuantityAsync(updateDto);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.NotFound);
    }

    #endregion

    #region MergeGuestCartToUserAsync Tests

    [Fact]
    public async Task MergeGuestCartToUserAsync_WithNoUserCart_ConvertsGuestCart()
    {
        // Arrange
        var sessionId = "guest-session-123";
        var userId = 1;

        var guestCartItem = TestDataBuilder.CartItem()
            .WithProductId(1)
            .WithQuantity(2)
            .Build();

        var guestCart = new Cart
        {
            Id = 1,
            SessionId = sessionId,
            CartItems = new List<CartItem> { guestCartItem }
        };

        _cartRepositoryMock.Setup(x => x.GetCartBySessionId(sessionId))
            .ReturnsAsync(guestCart);

        _cartRepositoryMock.Setup(x => x.GetCartByUserId(userId))
            .ReturnsAsync((Cart?)null); // No user cart exists

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var cartDto = new CartDTO { Id = 1, UserId = userId };
        _mapperMock.Setup(x => x.Map<CartDTO>(It.IsAny<Cart>()))
            .Returns(cartDto);

        // Act
        var result = await _cartService.MergeGuestCartToUserAsync(sessionId, userId);

        // Assert
        result.Should().BeSuccess();

        // Verify guest cart was converted to user cart
        _cartRepositoryMock.Verify(x => x.Update(It.Is<Cart>(c =>
            c.UserId == userId &&
            c.SessionId == null)), Times.Once);

        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task MergeGuestCartToUserAsync_WithBothCarts_MergesItems()
    {
        // Arrange
        var sessionId = "guest-session-123";
        var userId = 1;

        var product1 = TestDataBuilder.Product()
            .WithId(1)
            .WithStockQuantity(100)
            .Build();

        var product2 = TestDataBuilder.Product()
            .WithId(2)
            .WithStockQuantity(100)
            .Build();

        // Guest cart has product 1 (qty 3) and product 2 (qty 2)
        var guestCartItem1 = TestDataBuilder.CartItem()
            .WithProductId(1)
            .WithQuantity(3)
            .Build();
        guestCartItem1.Product = product1;

        var guestCartItem2 = TestDataBuilder.CartItem()
            .WithProductId(2)
            .WithQuantity(2)
            .Build();
        guestCartItem2.Product = product2;

        var guestCart = new Cart
        {
            Id = 1,
            SessionId = sessionId,
            CartItems = new List<CartItem> { guestCartItem1, guestCartItem2 }
        };

        // User cart already has product 1 (qty 5)
        var userCartItem = TestDataBuilder.CartItem()
            .WithProductId(1)
            .WithQuantity(5)
            .Build();

        var userCart = new Cart
        {
            Id = 2,
            UserId = userId,
            CartItems = new List<CartItem> { userCartItem }
        };

        _cartRepositoryMock.Setup(x => x.GetCartBySessionId(sessionId))
            .ReturnsAsync(guestCart);

        _cartRepositoryMock.Setup(x => x.GetCartByUserId(userId))
            .ReturnsAsync(userCart);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var cartDto = new CartDTO { Id = 2 };
        _mapperMock.Setup(x => x.Map<CartDTO>(It.IsAny<Cart>()))
            .Returns(cartDto);

        // Act
        var result = await _cartService.MergeGuestCartToUserAsync(sessionId, userId);

        // Assert
        result.Should().BeSuccess();

        // Verify items were updated
        _cartItemRepositoryMock.Verify(x => x.UpdateRange(It.IsAny<IEnumerable<CartItem>>()), Times.Once);

        // Verify guest cart was deleted
        _cartRepositoryMock.Verify(x => x.Delete(guestCart), Times.Once);

        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task MergeGuestCartToUserAsync_WithEmptyGuestCart_ReturnsUserCart()
    {
        // Arrange
        var sessionId = "guest-session-123";
        var userId = 1;

        var guestCart = new Cart
        {
            Id = 1,
            SessionId = sessionId,
            CartItems = new List<CartItem>() // Empty
        };

        _cartRepositoryMock.Setup(x => x.GetCartBySessionId(sessionId))
            .ReturnsAsync(guestCart);

        var cartDto = new CartDTO { UserId = userId };
        _mapperMock.Setup(x => x.Map<CartDTO>(It.IsAny<Cart>()))
            .Returns(cartDto);

        // Act
        var result = await _cartService.MergeGuestCartToUserAsync(sessionId, userId);

        // Assert
        result.Should().BeSuccess();

        // Verify no merge happened, just returned user cart
        _cartItemRepositoryMock.Verify(x => x.UpdateRange(It.IsAny<IEnumerable<CartItem>>()), Times.Never);
        _cartRepositoryMock.Verify(x => x.Delete(It.IsAny<Cart>()), Times.Never);
    }

    [Fact]
    public async Task MergeGuestCartToUserAsync_WithInvalidSessionId_ReturnsError()
    {
        // Arrange
        var sessionId = "";
        var userId = 1;

        // Act
        var result = await _cartService.MergeGuestCartToUserAsync(sessionId, userId);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.BadRequest);
    }

    [Fact]
    public async Task MergeGuestCartToUserAsync_WithInvalidUserId_ReturnsError()
    {
        // Arrange
        var sessionId = "guest-session-123";
        var userId = 0; // Invalid

        // Act
        var result = await _cartService.MergeGuestCartToUserAsync(sessionId, userId);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.BadRequest);
    }

    #endregion
}
