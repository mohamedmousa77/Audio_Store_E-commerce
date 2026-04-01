using AudioStore.Application.Services.Implementations;
using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Orders;
using AudioStore.Common.Enums;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Tests.Helpers;
using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioStore.Tests.UnitTests.Services;

/// <summary>
/// Unit tests for OrderService
/// Tests business logic for order status updates, cancellation,
/// stock restoration, and order validation
/// Note: CreateOrderAsync tests are complex due to Query() and transaction dependencies
/// </summary>
public class OrderServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<OrderService>> _loggerMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly OrderService _orderService;
    private readonly Mock<IPromoCodeService> _promoCodeService;
    private readonly Mock<IEmailService> _emailService;
    private readonly Mock<INotificationService> _notificationService;
    private readonly Mock<UserManager<User>> _userManager;


    public OrderServiceTests()
    {       
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<OrderService>>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _promoCodeService = new Mock<IPromoCodeService>();
        _emailService = new Mock<IEmailService>();
        _notificationService = new Mock<INotificationService>();
        _userManager = new Mock<UserManager<User>>(
            new Mock<Microsoft.AspNetCore.Identity.IUserStore<User>>().Object,
            null, null, null, null, null, null, null, null);

        _unitOfWorkMock.Setup(x => x.Orders).Returns(_orderRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Products).Returns(_productRepositoryMock.Object);

        // Setup transaction mocks
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.CommitTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.RollbackTransactionAsync()).Returns(Task.CompletedTask);

        _orderService = new OrderService(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _promoCodeService.Object,
            _emailService.Object,
            _notificationService.Object,
            _userManager.Object
            );
    }

    #region UpdateOrderStatusAsync Tests

    [Fact]
    public async Task UpdateOrderStatusAsync_WithValidTransition_UpdatesStatus()
    {
        // Arrange
        var order = TestDataBuilder.Order()
            .WithId(1)
            .WithStatus(OrderStatus.Processing)
            .Build();

        var updateDto = new UpdateOrderStatusDTO
        {
            OrderId = 1,
            NewStatus = OrderStatus.Shipped
        };

        _orderRepositoryMock.Setup(x => x.GetOrderById(1))
            .ReturnsAsync(order);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        var orderDto = new OrderDTO { Id = 1, OrderStatus = OrderStatus.Shipped };
        _mapperMock.Setup(x => x.Map<OrderDTO>(It.IsAny<Order>()))
            .Returns(orderDto);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(updateDto);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();

        _orderRepositoryMock.Verify(x => x.Update(It.Is<Order>(o =>
            o.Id == 1 &&
            o.Status == OrderStatus.Shipped)), Times.Once);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WithInvalidTransition_ReturnsError()
    {
        // Arrange
        var order = TestDataBuilder.Order()
            .WithId(1)
            .WithStatus(OrderStatus.Delivered) // Already delivered
            .Build();

        var updateDto = new UpdateOrderStatusDTO
        {
            OrderId = 1,
            NewStatus = OrderStatus.Processing // Can't go back to processing
        };

        _orderRepositoryMock.Setup(x => x.GetOrderById(1))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(updateDto);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.BadRequest);

        _orderRepositoryMock.Verify(x => x.Update(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrderStatusAsync_WithNonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateOrderStatusDTO
        {
            OrderId = 999,
            NewStatus = OrderStatus.Shipped
        };

        _orderRepositoryMock.Setup(x => x.GetOrderById(999))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _orderService.UpdateOrderStatusAsync(updateDto);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.OrderNotFound);
    }

    #endregion

    #region CancelOrderAsync Tests

    [Fact]
    public async Task CancelOrderAsync_WithValidOrder_CancelsAndRestoresStock()
    {
        // Arrange
        var product = TestDataBuilder.Product()
            .WithId(1)
            .WithStockQuantity(50)
            .Build();

        var orderItem = TestDataBuilder.OrderItem()
            .WithProductId(1)
            .WithQuantity(5)
            .Build();
        orderItem.Product = product;

        var order = TestDataBuilder.Order()
            .WithId(1)
            .WithStatus(OrderStatus.Processing)
            .Build();
        order.OrderItems.Add(orderItem);

        _orderRepositoryMock.Setup(x => x.GetOrderById(1))
            .ReturnsAsync(order);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _orderService.CancelOrderAsync(1);

        // Assert
        result.Should().BeSuccess();

        // Verify stock was restored
        _productRepositoryMock.Verify(x => x.Update(It.Is<Product>(p =>
            p.Id == 1 &&
            p.StockQuantity == 55)), Times.Once); // 50 + 5 = 55

        // Verify order was cancelled
        _orderRepositoryMock.Verify(x => x.Update(It.Is<Order>(o =>
            o.Id == 1 &&
            o.Status == OrderStatus.Cancelled)), Times.Once);

        _unitOfWorkMock.Verify(x => x.CommitTransactionAsync(), Times.Once);
    }

    [Fact]
    public async Task CancelOrderAsync_WithShippedOrder_ReturnsError()
    {
        // Arrange
        var order = TestDataBuilder.Order()
            .WithId(1)
            .WithStatus(OrderStatus.Shipped) // Already shipped
            .Build();

        _orderRepositoryMock.Setup(x => x.GetOrderById(1))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.CancelOrderAsync(1);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.BadRequest);

        _orderRepositoryMock.Verify(x => x.Update(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_WithUnauthorizedUser_ReturnsError()
    {
        // Arrange
        var order = TestDataBuilder.Order()
            .WithId(1)
            .WithUserId(1) // Order belongs to user 1
            .WithStatus(OrderStatus.Processing)
            .Build();

        _orderRepositoryMock.Setup(x => x.GetOrderById(1))
            .ReturnsAsync(order);

        // Act - User 2 trying to cancel user 1's order
        var result = await _orderService.CancelOrderAsync(1, userId: 2);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.Unauthorized);

        _orderRepositoryMock.Verify(x => x.Update(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_WithAlreadyCancelledOrder_ReturnsError()
    {
        // Arrange
        var order = TestDataBuilder.Order()
            .WithId(1)
            .WithStatus(OrderStatus.Cancelled) // Already cancelled
            .Build();

        _orderRepositoryMock.Setup(x => x.GetOrderById(1))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.CancelOrderAsync(1);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.BadRequest);

        _orderRepositoryMock.Verify(x => x.Update(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CancelOrderAsync_WithDeliveredOrder_ReturnsError()
    {
        // Arrange
        var order = TestDataBuilder.Order()
            .WithId(1)
            .WithStatus(OrderStatus.Delivered) // Already delivered
            .Build();

        _orderRepositoryMock.Setup(x => x.GetOrderById(1))
            .ReturnsAsync(order);

        // Act
        var result = await _orderService.CancelOrderAsync(1);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.BadRequest);
    }

    #endregion

    #region GetOrderByIdAsync Tests

    [Fact]
    public async Task GetOrderByIdAsync_WithExistingOrder_ReturnsOrder()
    {
        // Arrange
        var order = TestDataBuilder.Order()
            .WithId(1)
            .Build();

        var orderDto = new OrderDTO { Id = 1 };

        _orderRepositoryMock.Setup(x => x.GetOrderWithItemsAsync(1))
            .ReturnsAsync(order);

        _mapperMock.Setup(x => x.Map<OrderDTO>(order))
            .Returns(orderDto);

        // Act
        var result = await _orderService.GetOrderByIdAsync(1);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
    }

    [Fact]
    public async Task GetOrderByIdAsync_WithNonExistentOrder_ReturnsNotFound()
    {
        // Arrange
        _orderRepositoryMock.Setup(x => x.GetOrderWithItemsAsync(999))
            .ReturnsAsync((Order?)null);

        // Act
        var result = await _orderService.GetOrderByIdAsync(999);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.OrderNotFound);
    }

    #endregion

    #region Email Integration Tests

    /// <summary>
    /// Verifies that SendOrderConfirmationEmailAsync is NOT called
    /// when an order status is merely updated (email only fires on creation).
    /// This ensures the email trigger is scoped exclusively to CreateOrderAsync.
    /// </summary>
    [Fact]
    public async Task UpdateOrderStatusAsync_DoesNotTriggerEmail()
    {
        // Arrange
        var order = TestDataBuilder.Order()
            .WithId(1)
            .WithStatus(OrderStatus.Processing)
            .Build();

        var updateDto = new UpdateOrderStatusDTO
        {
            OrderId = 1,
            NewStatus = OrderStatus.Shipped
        };

        _orderRepositoryMock.Setup(x => x.GetOrderById(1))
            .ReturnsAsync(order);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<OrderDTO>(It.IsAny<Order>()))
            .Returns(new OrderDTO { Id = 1, OrderStatus = OrderStatus.Shipped });

        // Act
        await _orderService.UpdateOrderStatusAsync(updateDto);

        // Assert — email should NEVER be called on status update
        _emailService.Verify(
            e => e.SendOrderConfirmationEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<decimal>()),
            Times.Never,
            "Email should not be sent when merely updating order status");
    }

    /// <summary>
    /// Verifies that CancelOrderAsync does not trigger any email sending.
    /// Emails are only for creation confirmation, not cancellation.
    /// </summary>
    [Fact]
    public async Task CancelOrderAsync_DoesNotTriggerEmail()
    {
        // Arrange
        var product = TestDataBuilder.Product()
            .WithId(1)
            .WithStockQuantity(50)
            .Build();

        var orderItem = TestDataBuilder.OrderItem()
            .WithProductId(1)
            .WithQuantity(2)
            .Build();
        orderItem.Product = product;

        var order = TestDataBuilder.Order()
            .WithId(1)
            .WithStatus(OrderStatus.Processing)
            .Build();
        order.OrderItems.Add(orderItem);

        _orderRepositoryMock.Setup(x => x.GetOrderById(1))
            .ReturnsAsync(order);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _orderService.CancelOrderAsync(1);

        // Assert — email should NEVER fire on cancellation
        _emailService.Verify(
            e => e.SendOrderConfirmationEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<decimal>()),
            Times.Never,
            "Email should not be sent when cancelling an order");
    }

    #endregion
}
