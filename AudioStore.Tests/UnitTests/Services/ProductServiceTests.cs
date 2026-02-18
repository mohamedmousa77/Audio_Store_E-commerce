using AudioStore.Application.Services.Implementations;
using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Products;
using AudioStore.Common.Services.Interfaces;
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
/// Unit tests for ProductService
/// Tests business logic for product management including CRUD operations,
/// soft delete, and stock management
/// </summary>
public class ProductServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IImageStorageService> _imageStorageMock;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        _productRepositoryMock = new Mock<IProductRepository>();
        _imageStorageMock = new Mock<IImageStorageService>();

        _unitOfWorkMock.Setup(x => x.Products).Returns(_productRepositoryMock.Object);

        _productService = new ProductService(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object,
            _imageStorageMock.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsProduct()
    {
        // Arrange
        var productId = 1;
        var product = TestDataBuilder.Product()
            .WithId(productId)
            .WithName("Cuffie Sony WH-1000XM5")
            .WithPrice(399.99m)
            .InStock(25)
            .Build();

        var productDto = new ProductDTO
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        };

        _productRepositoryMock.Setup(x => x.GetProductById(productId))
            .ReturnsAsync(product);

        _mapperMock.Setup(x => x.Map<ProductDTO>(product))
            .Returns(productDto);

        // Act
        var result = await _productService.GetByIdAsync(productId);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(productId);
        result.Value.Name.Should().Be("Cuffie Sony WH-1000XM5");
        result.Value.Price.Should().Be(399.99m);
        result.Value.StockQuantity.Should().Be(25);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingId_ReturnsNotFound()
    {
        // Arrange
        var productId = 999;

        _productRepositoryMock.Setup(x => x.GetProductById(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.GetByIdAsync(productId);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.ProductNotFound);
    }

    #endregion

    #region DeleteAsync Tests (Soft Delete)

    [Fact]
    public async Task DeleteAsync_WithExistingProduct_SoftDeletesProduct()
    {
        // Arrange
        var productId = 1;
        var product = TestDataBuilder.Product()
            .WithId(productId)
            .WithName("Prodotto da Cancellare")
            .Build();

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _productService.DeleteAsync(productId);

        // Assert
        result.Should().BeSuccess();

        // Verify soft delete was called (sets IsDeleted = true)
        _productRepositoryMock.Verify(x => x.Update(It.Is<Product>(p =>
            p.Id == productId &&
            p.IsDeleted == true)), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var productId = 999;

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.DeleteAsync(productId);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.ProductNotFound);

        _productRepositoryMock.Verify(x => x.Update(It.IsAny<Product>()), Times.Never);
    }

    #endregion

    #region UpdateStockAsync Tests

    [Fact]
    public async Task UpdateStockAsync_WithValidQuantity_UpdatesStock()
    {
        // Arrange
        var productId = 1;
        var newQuantity = 50;
        var product = TestDataBuilder.Product()
            .WithId(productId)
            .WithStockQuantity(100)
            .Build();

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _productService.UpdateStockAsync(productId, newQuantity);

        // Assert
        result.Should().BeSuccess();

        _productRepositoryMock.Verify(x => x.Update(It.Is<Product>(p =>
            p.Id == productId &&
            p.StockQuantity == newQuantity)), Times.Once);
    }

    [Fact]
    public async Task UpdateStockAsync_WithNegativeQuantity_ReturnsError()
    {
        // Arrange
        var productId = 1;
        var product = TestDataBuilder.Product()
            .WithId(productId)
            .WithStockQuantity(10)
            .Build();

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        // Act
        var result = await _productService.UpdateStockAsync(productId, -5);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.InvalidQuantity);

        _productRepositoryMock.Verify(x => x.Update(It.IsAny<Product>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStockAsync_WithZeroQuantity_UpdatesStock()
    {
        // Arrange
        var productId = 1;
        var product = TestDataBuilder.Product()
            .WithId(productId)
            .WithStockQuantity(10)
            .Build();

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync(product);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _productService.UpdateStockAsync(productId, 0);

        // Assert
        result.Should().BeSuccess();

        _productRepositoryMock.Verify(x => x.Update(It.Is<Product>(p =>
            p.Id == productId &&
            p.StockQuantity == 0)), Times.Once);
    }

    [Fact]
    public async Task UpdateStockAsync_WithNonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var productId = 999;

        _productRepositoryMock.Setup(x => x.GetByIdAsync(productId))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.UpdateStockAsync(productId, 10);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.ProductNotFound);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesProduct()
    {
        // Arrange
        var request = new CreateProductDTO
        {
            Name = "Nuove Cuffie",
            Description = "Cuffie di alta qualità",
            Price = 199.99m,
            CategoryId = 1,
            StockQuantity = 100,
            Brand = "Sony"
        };

        var product = TestDataBuilder.Product()
            .WithId(1)
            .WithName(request.Name)
            .WithPrice(request.Price)
            .Build();

        var productDto = new ProductDTO
        {
            Id = 1,
            Name = request.Name,
            Price = request.Price
        };

        _mapperMock.Setup(x => x.Map<Product>(request))
            .Returns(product);

        _productRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Product>()))
            .ReturnsAsync(product);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<ProductDTO>(It.IsAny<Product>()))
            .Returns(productDto);

        // Act
        var result = await _productService.CreateAsync(request);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Nuove Cuffie");

        _productRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Product>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesProduct()
    {
        // Arrange
        var request = new UpdateProductDTO
        {
            Id = 1,
            Name = "Nome Aggiornato",
            Description = "Descrizione aggiornata",
            Price = 299.99m,
            CategoryId = 1,
            StockQuantity = 50
        };

        var existingProduct = TestDataBuilder.Product()
            .WithId(1)
            .WithName("Nome Vecchio")
            .WithPrice(199.99m)
            .Build();

        var updatedProductDto = new ProductDTO
        {
            Id = 1,
            Name = request.Name,
            Price = request.Price
        };

        _productRepositoryMock.Setup(x => x.GetProductById(request.Id))
            .ReturnsAsync(existingProduct);

        _mapperMock.Setup(x => x.Map(request, existingProduct))
            .Returns(existingProduct);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<ProductDTO>(It.IsAny<Product>()))
            .Returns(updatedProductDto);

        // Act
        var result = await _productService.UpdateAsync(request);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        result.Value!.Name.Should().Be("Nome Aggiornato");

        _productRepositoryMock.Verify(x => x.Update(It.IsAny<Product>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistingProduct_ReturnsNotFound()
    {
        // Arrange
        var request = new UpdateProductDTO
        {
            Id = 999,
            Name = "Test",
            Price = 99.99m,
            CategoryId = 1,
            StockQuantity = 10
        };

        _productRepositoryMock.Setup(x => x.GetProductById(request.Id))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _productService.UpdateAsync(request);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.ProductNotFound);

        _productRepositoryMock.Verify(x => x.Update(It.IsAny<Product>()), Times.Never);
    }

    #endregion
}
