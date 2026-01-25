using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Products;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AudioStore.Application.Services.Implementations;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ProductService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _mapper = mapper;
    }
    // ============ QUERIES ============
    public async Task<Result<ProductDTO>> GetByIdAsync(int id)
    {
        try
        {
            var product = await _unitOfWork.Products.GetProductById(id);
            if (product == null)
            {
                _logger.LogWarning("Product {ProductId} not found", id);
                return Result.Failure<ProductDTO>("Product not found",
                    ErrorCode.ProductNotFound);
            }
            var productDto = _mapper.Map<ProductDTO>(product);
            return Result.Success(productDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", id);
            return Result.Failure<ProductDTO>("Error getting the product",
                    ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<PaginatedResult<ProductDTO>>> GetAllAsync(ProductFilterDTO filter)
    {
        try
        {
            var query = _unitOfWork.Products
                .Query()
                .Include(p => p.Category)
                .AsQueryable();

            // Apply Filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                p.Name.ToLower().Trim().Contains(searchTerm) ||
                p.Brand.ToLower().Trim().Contains(searchTerm) ||
                p.Description.ToLower().Trim().Contains(searchTerm));
            }

            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

            if (!string.IsNullOrWhiteSpace(filter.Brand))
                query = query.Where(p => p.Brand == filter.Brand);

            if (filter.MinPrice.HasValue)
                query = query.Where(p => p.Price >= filter.MinPrice.Value);

            if (filter.MaxPrice.HasValue)
                query = query.Where(p => p.Price <= filter.MaxPrice.Value);

            if (filter.IsFeatured.HasValue)
                query = query.Where(p => p.IsFeatured == filter.IsFeatured.Value);

            if (filter.IsAvailable.HasValue)
                query = query.Where(p => p.IsAvailable == filter.IsAvailable.Value);

            // Apply sorting
            query = filter.SortBy?.ToLower() switch
            {
                "price_asc" => query.OrderBy(p => p.Price),
                "price_desc" => query.OrderByDescending(p => p.Price),
                "name" => query.OrderBy(p => p.Name),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                _ => query.OrderBy(p => p.Name)
            };

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var products = await query
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            var productDtos = _mapper.Map<List<ProductDTO>>(products);

            var result = new PaginatedResult<ProductDTO>
            {
                Items = productDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
            };
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products with filter");
            return Result.Failure<PaginatedResult<ProductDTO>>(
                "Errore recupero prodotti",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<ProductDTO>>> GetFeaturedAsync(int count = 10)
    {
        try
        {
            var products = await _unitOfWork.Products.GetFeaturedProducts(count);

            var productDtos = _mapper.Map<IEnumerable<ProductDTO>>(products);
            return Result.Success(productDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting featured products");
            return Result.Failure<IEnumerable<ProductDTO>>(
                "Errore recupero prodotti in evidenza",
                ErrorCode.InternalServerError);
        }

    }

    public async Task<Result<IEnumerable<ProductDTO>>> GetByCategoryAsync(int categoryId)
    {
        try
        {
            var products = await _unitOfWork.Products.GetProductsByCategory(categoryId);

            var productdtos = _mapper.Map<IEnumerable<ProductDTO>>(products);

            return Result.Success(productdtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products by category {CategoryId}", categoryId);
            return Result.Failure<IEnumerable<ProductDTO>>(
                "Errore recupero prodotti per categoria",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<ProductDTO>>> SearchAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Result.Success(Enumerable.Empty<ProductDTO>());

            var searchLowercase = searchTerm.ToLower().Trim();
            var products = await _unitOfWork.Products.GetAllFilteredProducts(searchLowercase);

            var productDtos = _mapper.Map<IEnumerable<ProductDTO>>(products);
            return Result.Success(productDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return Result.Failure<IEnumerable<ProductDTO>>(
                "Errore ricerca prodotti",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<IEnumerable<string>>> GetBrandsAsync()
    {
        try
        {
            var brands = await _unitOfWork.Products.GetAllBrands();
            if (brands == null)
                return Result.Failure<IEnumerable<string>>("Errore carica i brands", ErrorCode.BadRequest);


            return Result.Success(brands.AsEnumerable());


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting brands");
            return Result.Failure<IEnumerable<string>>(
                "Errore recupero brand",
                ErrorCode.InternalServerError);
        }
    }


    // ============ COMMANDS ============
    public async Task<Result<ProductDTO>> CreateAsync(CreateProductDTO dto)
    {
        try
        {
            var product = _mapper.Map<Product>(dto);
            product.CreatedAt = DateTime.UtcNow;

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Product {ProductId} created successfully", product.Id);

            var productDto = _mapper.Map<ProductDTO>(product);
            return Result.Success(productDto);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return Result.Failure<ProductDTO>(
                "Errore creazione prodotto",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<ProductDTO>> UpdateAsync(UpdateProductDTO dto)
    {
        try
        {
            var product = await _unitOfWork.Products.GetProductById(dto.Id);

            if (product == null)
            {
                return Result.Failure<ProductDTO>(
                    "Prodotto non trovato",
                    ErrorCode.ProductNotFound);
            }

            _mapper.Map(dto, product);
            product.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Product {ProductId} updated successfully", product.Id);

            var productDto = _mapper.Map<ProductDTO>(product);
            return Result.Success(productDto);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", dto.Id);
            return Result.Failure<ProductDTO>(
                "Errore aggiornamento prodotto",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
            {
                return Result.Failure(
                    "Prodotto non trovato",
                    ErrorCode.ProductNotFound);
            }

            // soft delete
            product.IsDeleted = true;
            product.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Product {ProductId} deleted successfully", id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return Result.Failure(
                "Errore eliminazione prodotto",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result> UpdateStockAsync(int id, int quantity)
    {
        try
        {
            var product = await _unitOfWork.Products.GetByIdAsync(id);
            if (product == null)
                return Result.Failure("Prodotto non trovato", ErrorCode.ProductNotFound);

            if (quantity < 0)
                return Result.Failure("Quantità non valida", ErrorCode.InvalidQuantity);

            product.StockQuantity = quantity;
            product.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
               "Stock updated for product {ProductId}: {Quantity}",
               id,
               quantity);

            return Result.Success();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product {ProductId}", id);
            return Result.Failure(
                "Errore aggiornamento stock",
                ErrorCode.InternalServerError);
        }

    }

}

