using AudioStore.Domain.Entities;
using AudioStore.Infrastructure.Repositories;
using AudioStore.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace AudioStore.Tests.UnitTests.Repositories;

public class RepositoryTests : IDisposable
{
    private readonly Infrastructure.Data.AppDbContext _context;
    private readonly Repository<Product> _productRepository;
    private readonly Repository<Category> _categoryRepository;

    public RepositoryTests()
    {
        _context = TestDbContextFactory.CreateInMemoryContext($"TestDb_{Guid.NewGuid()}");
        _productRepository = new Repository<Product>(_context);
        _categoryRepository = new Repository<Category>(_context);
        
        TestDbContextFactory.SeedTestData(_context);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnEntity_WhenEntityExists()
    {
        // Act
        var result = await _productRepository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Name.Should().Be("Test Headphones");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEntityDoesNotExist()
    {
        // Act
        var result = await _productRepository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenEntityIsDeleted()
    {
        // Arrange
        var product = await _productRepository.GetByIdAsync(1);
        product!.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _productRepository.GetByIdAsync(1);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllActiveEntities()
    {
        // Act
        var results = await _productRepository.GetAllAsync();

        // Assert
        results.Should().HaveCount(2);
        results.Should().AllSatisfy(p => p.IsDeleted.Should().BeFalse());
    }

    [Fact]
    public async Task GetAllAsync_ShouldNotReturnDeletedEntities()
    {
        // Arrange
        var product = await _productRepository.GetByIdAsync(1);
        product!.IsDeleted = true;
        await _context.SaveChangesAsync();

        // Act
        var results = await _productRepository.GetAllAsync();

        // Assert
        results.Should().HaveCount(1);
        results.Should().NotContain(p => p.Id == 1);
    }

    [Fact]
    public async Task AddAsync_ShouldAddEntity()
    {
        // Arrange
        var newProduct = new Product
        {
            Name = "New Product",
            Slug = "new-product",
            Description = "New Description",
            Price = 299.99m,
            CategoryId = 1,
            StockQuantity = 15,
            MainImage = "/images/new.jpg",
            Brand = "New Brand"
        };

        // Act
        var result = await _productRepository.AddAsync(newProduct);
        await _context.SaveChangesAsync();

        // Assert
        result.Should().NotBeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        
        var savedProduct = await _productRepository.GetByIdAsync(result.Id);
        savedProduct.Should().NotBeNull();
        savedProduct!.Name.Should().Be("New Product");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntity()
    {
        // Arrange
        var product = await _productRepository.GetByIdAsync(1);
        product!.Name = "Updated Name";
        product.Price = 149.99m;

        // Act
         _productRepository.Update(product);
        await _context.SaveChangesAsync();

        // Assert
        var updatedProduct = await _productRepository.GetByIdAsync(1);
        updatedProduct.Should().NotBeNull();
        updatedProduct!.Name.Should().Be("Updated Name");
        updatedProduct.Price.Should().Be(149.99m);
        updatedProduct.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldSoftDeleteEntity()
    {
        // Act
        //_productRepository.Delete(1);
        await _context.SaveChangesAsync();

        // Assert
        var deletedProduct = await _productRepository.GetByIdAsync(1);
        deletedProduct.Should().BeNull();

        // Verify it's soft deleted, not hard deleted
        var productInDb = await _context.Products.FindAsync(1);
        productInDb.Should().NotBeNull();
        productInDb!.IsDeleted.Should().BeTrue();
        productInDb.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task FindAsync_ShouldReturnMatchingEntities()
    {
        // Act
        var results = await _productRepository.FindAsync(p => p.CategoryId == 1);

        // Assert
        results.Should().HaveCount(1);
        results.First().CategoryId.Should().Be(1);
    }

    [Fact]
    public async Task FirstOrDefaultAsync_ShouldReturnFirstMatch()
    {
        // Act
        var result = await _productRepository.FirstOrDefaultAsync(p => p.CategoryId == 1);

        // Assert
        result.Should().NotBeNull();
        result!.CategoryId.Should().Be(1);
    }

    [Fact]
    public async Task AnyAsync_ShouldReturnTrue_WhenEntityExists()
    {
        // Act
        var result = await _productRepository.AnyAsync(p => p.Name == "Test Headphones");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_ShouldReturnFalse_WhenEntityDoesNotExist()
    {
        // Act
        var result = await _productRepository.AnyAsync(p => p.Name == "Non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        // Act
        var totalCount = await _productRepository.CountAsync();
        var categoryCount = await _productRepository.CountAsync(p => p.CategoryId == 1);

        // Assert
        totalCount.Should().Be(2);
        categoryCount.Should().Be(1);
    }

    [Fact]
    public void Query_ShouldReturnQueryable()
    {
        // Act
        var query = _productRepository.Query();

        // Assert
        query.Should().NotBeNull();
        query.Count().Should().Be(2);
    }

    [Fact]
    public void QueryNoTracking_ShouldReturnQueryableWithNoTracking()
    {
        // Act
        var query = _productRepository.QueryNoTracking();
        var products = query.ToList();

        // Assert
        products.Should().HaveCount(2);
        
        // Verify no tracking
        _context.ChangeTracker.Entries<Product>().Should().BeEmpty();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
