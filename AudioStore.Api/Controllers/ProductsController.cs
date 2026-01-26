using Asp.Versioning;
using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Products;
using AudioStore.Common.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AudioStore.Api.Controllers;

/// <summary>
/// Products management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Get all products with optional filtering and pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PaginatedResult<ProductDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] ProductFilterDTO filter)
    {
        _logger.LogInformation("Getting products with filter");

        var result = await _productService.GetAllAsync(filter);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProductDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(int id)
    {
        _logger.LogInformation("Getting product by ID: {ProductId}", id);

        var result = await _productService.GetByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Get featured products
    /// </summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ProductDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFeaturedProducts([FromQuery] int count = 10)
    {
        _logger.LogInformation("Getting {Count} featured products", count);

        var result = await _productService.GetFeaturedAsync(count);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    [HttpGet("category/{categoryId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ProductDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductsByCategory(int categoryId)
    {
        _logger.LogInformation("Getting products for category: {CategoryId}", categoryId);

        var result = await _productService.GetByCategoryAsync(categoryId);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get all product brands
    /// </summary>
    [HttpGet("brands")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBrands()
    {
        _logger.LogInformation("Getting all product brands");

        var result = await _productService.GetBrandsAsync();

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Search products by term
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<ProductDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchProducts([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(new { error = "Search query is required" });
        }

        _logger.LogInformation("Searching products with query: {Query}", q);

        var result = await _productService.SearchAsync(q);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Create new product (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(ProductDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductDTO dto)
    {
        _logger.LogInformation("Creating new product: {ProductName}", dto.Name);

        var result = await _productService.CreateAsync(dto);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetProductById),
                new { id = result.Value!.Id },
                result.Value);
        }

        return StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Update existing product (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(ProductDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDTO dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "ID mismatch" });
        }

        _logger.LogInformation("Updating product: {ProductId}", id);

        var result = await _productService.UpdateAsync(dto);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Delete product (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        _logger.LogInformation("Deleting product: {ProductId}", id);

        var result = await _productService.DeleteAsync(id);

        return result.IsSuccess
            ? NoContent()
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Update product stock (Admin only)
    /// </summary>
    [HttpPatch("{id}/stock")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] int quantity)
    {
        if (quantity < 0)
        {
            return BadRequest(new { error = "Quantity cannot be negative" });
        }

        _logger.LogInformation("Updating stock for product {ProductId} to {Quantity}", id, quantity);

        var result = await _productService.UpdateStockAsync(id, quantity);

        return result.IsSuccess
            ? Ok(new { message = "Stock updated successfully" })
            : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
