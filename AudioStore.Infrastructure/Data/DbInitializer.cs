using AudioStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AudioStore.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(
        AppDbContext context,
        UserManager<User> userManager,
        RoleManager<IdentityRole<int>> roleManager,
        ILogger logger)
    {
        try
        {
            // Ensure database is created
            await context.Database.MigrateAsync();

            logger.LogInformation("Starting database seeding...");

            // Seed in order of dependencies
            await SeedRolesAsync(roleManager, logger);
            await SeedAdminUserAsync(userManager, logger);
            await SeedCategoriesAsync(context, logger);
            await SeedProductsAsync(context, logger);

            logger.LogInformation("Database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager, ILogger logger)
    {
        string[] roleNames = { "Administrator", "Customer", "Guest" };

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                var role = new IdentityRole<int> { Name = roleName };
                var result = await roleManager.CreateAsync(role);

                if (result.Succeeded)
                {
                    logger.LogInformation("Role '{RoleName}' created successfully", roleName);
                }
                else
                {
                    logger.LogWarning("Failed to create role '{RoleName}': {Errors}",
                        roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                logger.LogInformation("Role '{RoleName}' already exists", roleName);
            }
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<User> userManager, ILogger logger)
    {
        const string adminEmail = "admin@audiostore.com";
        const string adminPassword = "Admin@123456";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                RegistrationDate = DateTime.UtcNow,
                IsActive = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Administrator");
                logger.LogInformation("Admin user created successfully: {Email}", adminEmail);
                logger.LogInformation("Admin credentials - Email: {Email}, Password: {Password}",
                    adminEmail, adminPassword);
            }
            else
            {
                logger.LogWarning("Failed to create admin user: {Errors}",
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            logger.LogInformation("Admin user already exists: {Email}", adminEmail);
        }
    }

    private static async Task SeedCategoriesAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Categories.AnyAsync())
        {
            logger.LogInformation("Categories already exist, skipping seed");
            return;
        }

        var categories = new List<Category>
        {
            new Category
            {
                Name = "Headphones",
                Description = "Over-ear and on-ear headphones for immersive audio experience",
                Slug = "headphones",
                ImageUrl = "/images/categories/headphones.jpg",
                CreatedAt = DateTime.UtcNow
            },
            new Category
            {
                Name = "Speakers",
                Description = "Portable and home speakers for powerful sound",
                Slug = "speakers",
                ImageUrl = "/images/categories/speakers.jpg",
                CreatedAt = DateTime.UtcNow
            },
            new Category
            {
                Name = "Earphones",
                Description = "In-ear earphones and wireless earbuds",
                Slug = "earphones",
                ImageUrl = "/images/categories/earphones.jpg",
                CreatedAt = DateTime.UtcNow
            },
            new Category
            {
                Name = "Wireless",
                Description = "Wireless audio devices with Bluetooth connectivity",
                Slug = "wireless",
                ImageUrl = "/images/categories/wireless.jpg",
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} categories", categories.Count);
    }

    private static async Task SeedProductsAsync(AppDbContext context, ILogger logger)
    {
        if (await context.Products.AnyAsync())
        {
            logger.LogInformation("Products already exist, skipping seed");
            return;
        }

        // Get category IDs (they should exist from previous seed)
        var headphonesCategory = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "headphones");
        var speakersCategory = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "speakers");
        var earphonesCategory = await context.Categories.FirstOrDefaultAsync(c => c.Slug == "earphones");

        if (headphonesCategory == null || speakersCategory == null || earphonesCategory == null)
        {
            logger.LogWarning("Categories not found, cannot seed products");
            return;
        }

        var products = new List<Product>
        {
            // Headphones
            new Product
            {
                Name = "XX99 Mark II Headphones",
                Brand = "AudioStore",
                Description = "The new XX99 Mark II headphones is the pinnacle of pristine audio. It redefines your premium headphone experience by reproducing the balanced depth and precision of studio-quality sound.",
                Features = "Featuring a genuine leather head strap and premium earcups, these headphones deliver superior comfort for those who like to enjoy endless listening. It includes intuitive controls designed for any situation.",
                Price = 2999.00m,
                CategoryId = headphonesCategory.Id,
                StockQuantity = 15,
                MainImage = "/images/products/xx99-mark-two-headphones.jpg",
                IsNewProduct = true,
                IsFeatured = true,
                IsPublished = true,
                Slug = "xx99-mark-ii-headphones",
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "XX99 Mark I Headphones",
                Brand = "AudioStore",
                Description = "As the gold standard for headphones, the classic XX99 Mark I offers detailed and accurate audio reproduction for audiophiles, mixing engineers, and music aficionados alike in studios and on the go.",
                Features = "These headphones have been created from scratch with premium materials. Their design is both functional and stylish, with a focus on delivering the best possible listening experience.",
                Price = 1750.00m,
                CategoryId = headphonesCategory.Id,
                StockQuantity = 20,
                MainImage = "/images/products/xx99-mark-one-headphones.jpg",
                IsNewProduct = false,
                IsFeatured = true,
                IsPublished = true,
                Slug = "xx99-mark-i-headphones",
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "XX59 Headphones",
                Brand = "AudioStore",
                Description = "Enjoy your audio almost anywhere and customize it to your specific tastes with the XX59 headphones. The stylish yet durable versatile wireless headset is a brilliant companion at home or on the move.",
                Features = "The XX59 headphones combine stylish design with premium audio quality. They feature soft ear cushions and a lightweight design for extended listening sessions.",
                Price = 899.00m,
                CategoryId = headphonesCategory.Id,
                StockQuantity = 30,
                MainImage = "/images/products/xx59-headphones.jpg",
                IsNewProduct = false,
                IsFeatured = false,
                IsPublished = true,
                Slug = "xx59-headphones",
                CreatedAt = DateTime.UtcNow
            },

            // Speakers
            new Product
            {
                Name = "ZX9 Speaker",
                Brand = "AudioStore",
                Description = "Upgrade your sound system with the all new ZX9 active speaker. It's a bookshelf speaker system that offers truly wireless connectivity -- creating new possibilities for more pleasing and practical audio setups.",
                Features = "Connect via Bluetooth or nearly any wired source. This speaker features optical, digital coaxial, USB Type-B, stereo RCA, and stereo XLR inputs, allowing you to have up to five wired source devices connected for easy switching.",
                Price = 4500.00m,
                CategoryId = speakersCategory.Id,
                StockQuantity = 10,
                MainImage = "/images/products/zx9-speaker.jpg",
                IsNewProduct = true,
                IsFeatured = true,
                IsPublished = true,
                Slug = "zx9-speaker",
                CreatedAt = DateTime.UtcNow
            },
            new Product
            {
                Name = "ZX7 Speaker",
                Brand = "AudioStore",
                Description = "Stream high quality sound wirelessly with minimal loss. The ZX7 bookshelf speaker uses high-end audiophile components that represents the top of the line powered speakers for home or studio use.",
                Features = "Reap the advantages of a flat diaphragm tweeter cone. This provides a fast response rate and excellent high frequencies that lower tiered bookshelf speakers cannot provide.",
                Price = 3500.00m,
                CategoryId = speakersCategory.Id,
                StockQuantity = 12,
                MainImage = "/images/products/zx7-speaker.jpg",
                IsNewProduct = false,
                IsFeatured = true,
                IsPublished = true,
                Slug = "zx7-speaker",
                CreatedAt = DateTime.UtcNow
            },

            // Earphones
            new Product
            {
                Name = "YX1 Wireless Earphones",
                Brand = "AudioStore",
                Description = "Tailor your listening experience with bespoke dynamic drivers from the new YX1 Wireless Earphones. Enjoy incredible high-fidelity sound even in noisy environments with its active noise cancellation feature.",
                Features = "Experience unrivalled stereo sound thanks to innovative acoustic technology. With improved ergonomics designed for full day wearing, these revolutionary earphones have been finely crafted to provide you with the perfect fit.",
                Price = 599.00m,
                CategoryId = earphonesCategory.Id,
                StockQuantity = 50,
                MainImage = "/images/products/yx1-earphones.jpg",
                IsNewProduct = true,
                IsFeatured = false,
                IsPublished = true,
                Slug = "yx1-wireless-earphones",
                CreatedAt = DateTime.UtcNow
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();

        logger.LogInformation("Seeded {Count} products", products.Count);
    }
}
