using AudioStore.Domain.Entities;
using AudioStore.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AudioStore.Tests.Helpers;

public static class TestDbContextFactory
{
    public static AppDbContext CreateInMemoryContext(string databaseName = "TestDatabase")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new AppDbContext(options);
        
        // Ensure database is created
        context.Database.EnsureCreated();
        
        return context;
    }

    public static void SeedTestData(AppDbContext context)
    {
        // Seed Categories
        var categories = new List<Category>
        {
            new Category
            {
                Id = 1,
                Name = "Headphones",
                Slug = "headphones",
                ImageUrl = "/images/headphones.jpg",
                CreatedAt = DateTime.UtcNow
            },
            new Category
            {
                Id = 2,
                Name = "Speakers",
                Slug = "speakers",
                ImageUrl = "/images/speakers.jpg",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Categories.AddRange(categories);

        // Seed Products
        var products = new List<Product>
        {
            new Product
            {
                Id = 1,
                Name = "Test Headphones",
                Slug = "test-headphones",
                Description = "Test Description",
                Price = 99.99m,
                CategoryId = 1,
                StockQuantity = 10,
                MainImage = "/images/test.jpg",
                Brand = "Test Brand",
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Id = 2,
                Name = "Test Speakers",
                Slug = "test-speakers",
                Description = "Test Description",
                Price = 199.99m,
                CategoryId = 2,
                StockQuantity = 5,
                MainImage = "/images/test2.jpg",
                Brand = "Test Brand",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}
