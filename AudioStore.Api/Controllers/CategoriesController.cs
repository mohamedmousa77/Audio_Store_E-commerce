using Asp.Versioning;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Category;
using AudioStore.Common.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AudioStore.Api.Controllers;

/// <summary>
/// Categories management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(
        ICategoryService categoryService,
        ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IEnumerable<CategoryDTO>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories()
    {
        _logger.LogInformation("Getting all categories");

        var result = await _categoryService.GetAllAsync();

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        _logger.LogInformation("Getting category by ID: {CategoryId}", id);

        var result = await _categoryService.GetByIdAsync(id);

        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Create new category (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCategory([FromBody] CategoryDTO dto)
    {
        _logger.LogInformation("Creating new category: {CategoryName}", dto.Name);

        var result = await _categoryService.CreateAsync(dto);

        if (result.IsSuccess)
        {
            return CreatedAtAction(
                nameof(GetCategoryById),
                new { id = result.Value!.Id },
                result.Value);
        }

        return StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Update existing category (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDTO dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { error = "ID mismatch" });
        }

        _logger.LogInformation("Updating category: {CategoryId}", id);

        var result = await _categoryService.UpdateAsync(dto);

        return result.IsSuccess
            ? Ok(result.Value)
            : StatusCode(result.StatusCode, new { error = result.Error });
    }

    /// <summary>
    /// Delete category (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = UserRole.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        _logger.LogInformation("Deleting category: {CategoryId}", id);

        var result = await _categoryService.DeleteAsync(id);

        return result.IsSuccess
            ? NoContent()
            : StatusCode(result.StatusCode, new { error = result.Error });
    }
}
