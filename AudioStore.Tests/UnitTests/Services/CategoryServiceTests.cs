using AudioStore.Application.Services.Implementations;
using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Category;
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
/// Unit tests for CategoryService
/// Tests CRUD operations for category management
/// </summary>
public class CategoryServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ILogger<CategoryDTO>> _loggerMock;
    private readonly Mock<IRepository<Category>> _categoryRepositoryMock;
    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly CategoryService _categoryService;

    public CategoryServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _loggerMock = new Mock<ILogger<CategoryDTO>>();
        _categoryRepositoryMock = new Mock<IRepository<Category>>();
        _productRepositoryMock = new Mock<IProductRepository>();

        _unitOfWorkMock.Setup(x => x.Categories).Returns(_categoryRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Products).Returns(_productRepositoryMock.Object);

        _categoryService = new CategoryService(
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _loggerMock.Object);
    }

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllCategories()
    {
        // Arrange
        var categories = new List<Category>
        {
            TestDataBuilder.Category().WithId(1).WithName("Cuffie").Build(),
            TestDataBuilder.Category().WithId(2).WithName("Altoparlanti").Build()
        };

        var categoryDtos = new List<CategoryDTO>
        {
            new CategoryDTO { Id = 1, Name = "Cuffie" },
            new CategoryDTO { Id = 2, Name = "Altoparlanti" }
        };

        _categoryRepositoryMock.Setup(x => x.GetAllAsync())
            .ReturnsAsync(categories);

        _mapperMock.Setup(x => x.Map<IEnumerable<CategoryDTO>>(categories))
            .Returns(categoryDtos);

        // Act
        var result = await _categoryService.GetAllAsync();

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ReturnsCategory()
    {
        // Arrange
        var category = TestDataBuilder.Category()
            .WithId(1)
            .WithName("Cuffie")
            .Build();

        var categoryDto = new CategoryDTO { Id = 1, Name = "Cuffie" };

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(category);

        _mapperMock.Setup(x => x.Map<CategoryDTO>(category))
            .Returns(categoryDto);

        // Act
        var result = await _categoryService.GetByIdAsync(1);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
        result.Value.Name.Should().Be("Cuffie");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ReturnsNotFound()
    {
        // Arrange
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _categoryService.GetByIdAsync(999);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.NotFound);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCategory()
    {
        // Arrange
        var categoryDto = new CategoryDTO
        {
            Name = "Nuova Categoria",
            Description = "Descrizione test"
        };

        var category = TestDataBuilder.Category()
            .WithId(1)
            .WithName("Nuova Categoria")
            .Build();

        _mapperMock.Setup(x => x.Map<Category>(categoryDto))
            .Returns(category);

        _categoryRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Category>()))
            .ReturnsAsync(category);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<CategoryDTO>(It.IsAny<Category>()))
            .Returns(new CategoryDTO { Id = 1, Name = "Nuova Categoria" });

        // Act
        var result = await _categoryService.CreateAsync(categoryDto);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();

        _categoryRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Category>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesCategory()
    {
        // Arrange
        var existingCategory = TestDataBuilder.Category()
            .WithId(1)
            .WithName("Nome Vecchio")
            .Build();

        var updateDto = new CategoryDTO
        {
            Id = 1,
            Name = "Nome Aggiornato",
            Description = "Descrizione aggiornata"
        };

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(existingCategory);

        _mapperMock.Setup(x => x.Map(updateDto, existingCategory))
            .Returns(existingCategory);

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        _mapperMock.Setup(x => x.Map<CategoryDTO>(It.IsAny<Category>()))
            .Returns(new CategoryDTO { Id = 1, Name = "Nome Aggiornato" });

        // Act
        var result = await _categoryService.UpdateAsync(updateDto);

        // Assert
        result.Should().BeSuccess();
        result.Should().HaveData();

        _categoryRepositoryMock.Verify(x => x.Update(It.IsAny<Category>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentCategory_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new CategoryDTO
        {
            Id = 999,
            Name = "Test"
        };

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _categoryService.UpdateAsync(updateDto);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.NotFound);

        _categoryRepositoryMock.Verify(x => x.Update(It.IsAny<Category>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithEmptyCategory_DeletesCategory()
    {
        // Arrange
        var category = TestDataBuilder.Category()
            .WithId(1)
            .Build();

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(category);

        _productRepositoryMock.Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(false); // No products in category

        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        var result = await _categoryService.DeleteAsync(1);

        // Assert
        result.Should().BeSuccess();

        _categoryRepositoryMock.Verify(x => x.Delete(category), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithProductsInCategory_ReturnsError()
    {
        // Arrange
        var category = TestDataBuilder.Category()
            .WithId(1)
            .Build();

        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(category);

        _productRepositoryMock.Setup(x => x.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>()))
            .ReturnsAsync(true); // Has products

        // Act
        var result = await _categoryService.DeleteAsync(1);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.BadRequest);

        _categoryRepositoryMock.Verify(x => x.Delete(It.IsAny<Category>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentCategory_ReturnsNotFound()
    {
        // Arrange
        _categoryRepositoryMock.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Category?)null);

        // Act
        var result = await _categoryService.DeleteAsync(999);

        // Assert
        result.Should().BeFailure();
        result.Should().HaveErrorCode(ErrorCode.NotFound);
    }

    #endregion
}
