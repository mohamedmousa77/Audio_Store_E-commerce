using AudioStore.Application.Services.Implementations;
using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Admin.Dashboard;
using AudioStore.Common.Enums;
using AudioStore.Domain.Interfaces;
using AudioStore.Tests.Helpers;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AudioStore.Tests.UnitTests.Services;

/// <summary>
/// Unit tests for DashboardService
/// Tests analytics and statistics aggregation
/// </summary>
public class DashboardServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<DashboardService>> _loggerMock;
    private readonly Mock<IDashboardRepository> _dashboardRepositoryMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly DashboardService _dashboardService;

    public DashboardServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<DashboardService>>();
        _dashboardRepositoryMock = new Mock<IDashboardRepository>();
        _orderRepositoryMock = new Mock<IOrderRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();

        _unitOfWorkMock.Setup(x => x.Dashboard).Returns(_dashboardRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Orders).Returns(_orderRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Users).Returns(_userRepositoryMock.Object);

        _dashboardService = new DashboardService(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    #region GetDashboardStatsAsync Tests

    [Fact]
    public async Task GetDashboardStatsAsync_ReturnsCompleteStatistics()
    {
        // Arrange
        var totalSales = 15000.50m;
        var totalOrders = 42;
        var totalCustomers = 25;

        var ordersByStatus = new Dictionary<OrderStatus, int>
        {
            { OrderStatus.Pending, 5 },
            { OrderStatus.Processing, 10 },
            { OrderStatus.Shipped, 8 },
            { OrderStatus.Delivered, 15 },
            { OrderStatus.Cancelled, 4 }
        };

        var topProductsData = new List<TopProductData>
        {
            new TopProductData { ProductId = 1, ProductName = "Cuffie Wireless", TotalQuantitySold = 50, TotalRevenue = 2500m },
            new TopProductData { ProductId = 2, ProductName = "Speaker Bluetooth", TotalQuantitySold = 30, TotalRevenue = 1500m }
        };

        var topProducts = new List<TopProductDTO>
        {
            new TopProductDTO { ProductId = 1, ProductName = "Cuffie Wireless", TotalQuantitySold = 50, TotalRevenue = 2500m },
            new TopProductDTO { ProductId = 2, ProductName = "Speaker Bluetooth", TotalQuantitySold = 30, TotalRevenue = 1500m }
        };

        var topCategoriesData = new List<TopCategoryData>
        {
            new TopCategoryData { CategoryId = 1, CategoryName = "Cuffie", TotalQuantitySold = 100, TotalRevenue = 5000m }
        };

        var topCategories = new List<TopCategoryDTO>
        {
            new TopCategoryDTO { CategoryId = 1, CategoryName = "Cuffie", TotalQuantitySold = 100, TotalRevenue = 5000m }
        };

        _dashboardRepositoryMock.Setup(x => x.GetTotalSalesAsync())
            .ReturnsAsync(totalSales);

        _orderRepositoryMock.Setup(x => x.CountAsync(null, default))
            .ReturnsAsync(totalOrders);

        _dashboardRepositoryMock.Setup(x => x.GetOrdersByStatusAsync())
            .ReturnsAsync(ordersByStatus);

        _userRepositoryMock.Setup(x => x.GetTotalCustomersCountAsync())
            .ReturnsAsync(totalCustomers);

        _dashboardRepositoryMock.Setup(x => x.GetTopProductsAsync(5))
            .ReturnsAsync(topProductsData);

        _mapperMock.Setup(x => x.Map<List<TopProductDTO>>(topProductsData))
            .Returns(topProducts);

        _dashboardRepositoryMock.Setup(x => x.GetTopCategoriesAsync(3))
            .ReturnsAsync(topCategoriesData);

        _mapperMock.Setup(x => x.Map<List<TopCategoryDTO>>(topCategoriesData))
            .Returns(topCategories);

        // Act
        var result = await _dashboardService.GetDashboardStatsAsync();

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        
        result.Value!.TotalSales.Should().Be(totalSales);
        result.Value.TotalOrders.Should().Be(totalOrders);
        result.Value.TotalCustomers.Should().Be(totalCustomers);
        
        result.Value.OrdersByStatus.Should().NotBeNull();
        result.Value.OrdersByStatus.Pending.Should().Be(5);
        result.Value.OrdersByStatus.Processing.Should().Be(10);
        result.Value.OrdersByStatus.Shipped.Should().Be(8);
        result.Value.OrdersByStatus.Delivered.Should().Be(15);
        result.Value.OrdersByStatus.Cancelled.Should().Be(4);
        
        result.Value.TopProducts.Should().HaveCount(2);
        result.Value.TopCategories.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithNoOrders_ReturnsZeroStatistics()
    {
        // Arrange
        var emptyOrdersByStatus = new Dictionary<OrderStatus, int>();

        _dashboardRepositoryMock.Setup(x => x.GetTotalSalesAsync())
            .ReturnsAsync(0m);

        _orderRepositoryMock.Setup(x => x.CountAsync(null, default))
            .ReturnsAsync(0);

        _dashboardRepositoryMock.Setup(x => x.GetOrdersByStatusAsync())
            .ReturnsAsync(emptyOrdersByStatus);

        _userRepositoryMock.Setup(x => x.GetTotalCustomersCountAsync())
            .ReturnsAsync(0);

        _dashboardRepositoryMock.Setup(x => x.GetTopProductsAsync(5))
            .ReturnsAsync(new List<TopProductData>());

        _mapperMock.Setup(x => x.Map<List<TopProductDTO>>(It.IsAny<IEnumerable<TopProductData>>()))
            .Returns(new List<TopProductDTO>());

        _dashboardRepositoryMock.Setup(x => x.GetTopCategoriesAsync(3))
            .ReturnsAsync(new List<TopCategoryData>());

        _mapperMock.Setup(x => x.Map<List<TopCategoryDTO>>(It.IsAny<IEnumerable<TopCategoryData>>()))
            .Returns(new List<TopCategoryDTO>());

        // Act
        var result = await _dashboardService.GetDashboardStatsAsync();

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        
        result.Value!.TotalSales.Should().Be(0m);
        result.Value.TotalOrders.Should().Be(0);
        result.Value.TotalCustomers.Should().Be(0);
        
        result.Value.OrdersByStatus.Pending.Should().Be(0);
        result.Value.TopProducts.Should().BeEmpty();
        result.Value.TopCategories.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDashboardStatsAsync_WithPartialOrderStatuses_FillsMissingWithZero()
    {
        // Arrange
        var partialOrdersByStatus = new Dictionary<OrderStatus, int>
        {
            { OrderStatus.Processing, 10 },
            { OrderStatus.Delivered, 5 }
            // Missing: Pending, Shipped, Cancelled
        };

        _dashboardRepositoryMock.Setup(x => x.GetTotalSalesAsync())
            .ReturnsAsync(1000m);

        _orderRepositoryMock.Setup(x => x.CountAsync(null, default))
            .ReturnsAsync(15);

        _dashboardRepositoryMock.Setup(x => x.GetOrdersByStatusAsync())
            .ReturnsAsync(partialOrdersByStatus);

        _userRepositoryMock.Setup(x => x.GetTotalCustomersCountAsync())
            .ReturnsAsync(10);

        _dashboardRepositoryMock.Setup(x => x.GetTopProductsAsync(5))
            .ReturnsAsync(new List<TopProductData>());

        _mapperMock.Setup(x => x.Map<List<TopProductDTO>>(It.IsAny<IEnumerable<TopProductData>>()))
            .Returns(new List<TopProductDTO>());

        _dashboardRepositoryMock.Setup(x => x.GetTopCategoriesAsync(3))
            .ReturnsAsync(new List<TopCategoryData>());

        _mapperMock.Setup(x => x.Map<List<TopCategoryDTO>>(It.IsAny<IEnumerable<TopCategoryData>>()))
            .Returns(new List<TopCategoryDTO>());

        // Act
        var result = await _dashboardService.GetDashboardStatsAsync();

        // Assert
        result.Should().BeSuccess();
        result.Value.Should().NotBeNull();
        
        result.Value!.OrdersByStatus.Pending.Should().Be(0);
        result.Value.OrdersByStatus.Processing.Should().Be(10);
        result.Value.OrdersByStatus.Shipped.Should().Be(0);
        result.Value.OrdersByStatus.Delivered.Should().Be(5);
        result.Value.OrdersByStatus.Cancelled.Should().Be(0);
    }

    #endregion
}
