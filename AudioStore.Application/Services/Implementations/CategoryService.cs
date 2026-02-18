using System.Text.RegularExpressions;
using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Category;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AudioStore.Application.Services.Implementations;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoryDTO> _logger;
    private readonly IImageStorageService _imageStorage;

    public CategoryService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CategoryDTO> logger, IImageStorageService imageStorage)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _imageStorage = imageStorage;
    }
    public async Task<Result<IEnumerable<CategoryDTO>>> GetAllAsync()
    {
        try
        {
            var categories = await _unitOfWork.Categories.GetAllAsync();
            var categoryDtos = _mapper.Map<IEnumerable<CategoryDTO>>(categories);
            return Result.Success(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return Result.Failure<IEnumerable<CategoryDTO>>(
                "Errore recupero categorie",
                ErrorCode.InternalServerError);
        }
    }
    public async Task<Result<CategoryDTO>> GetByIdAsync(int id)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
                return Result.Failure<CategoryDTO>("Categoria non trovata", ErrorCode.NotFound);

            var categoryDto = _mapper.Map<CategoryDTO>(category);
            return Result.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting category {CategoryId}", id);
            return Result.Failure<CategoryDTO>(
                "Errore recupero categoria",
                ErrorCode.InternalServerError);
        }
    }
    public async Task<Result<CategoryDTO>> CreateAsync(CategoryDTO dto)
    {
        try
        {
            var category = _mapper.Map<Category>(dto);
            category.CreatedAt = DateTime.UtcNow;

            // Auto-generate slug from name
            category.Slug = GenerateSlug(dto.Name);

            // Save image to disk if it's base64
            if (!string.IsNullOrEmpty(category.ImageUrl))
                category.ImageUrl = await _imageStorage.SaveImageAsync(category.ImageUrl, "categories");

            await _unitOfWork.Categories.AddAsync(category);
            await _unitOfWork.SaveChangesAsync();

            var categoryDto = _mapper.Map<CategoryDTO>(category);
            return Result.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            return Result.Failure<CategoryDTO>(
                "Errore creazione categoria",
                ErrorCode.InternalServerError);
        }
    }
    public async Task<Result<CategoryDTO>> UpdateAsync(CategoryDTO dto)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(dto.Id);
            if (category == null)
                return Result.Failure<CategoryDTO>("Categoria non trovata", ErrorCode.NotFound);

            var oldImageUrl = category.ImageUrl;

            _mapper.Map(dto, category);
            category.UpdatedAt = DateTime.UtcNow;
            category.Slug = GenerateSlug(dto.Name);

            // Save new image to disk if it's base64
            if (!string.IsNullOrEmpty(category.ImageUrl))
                category.ImageUrl = await _imageStorage.SaveImageAsync(category.ImageUrl, "categories");

            // Clean up old image if replaced
            if (!string.IsNullOrEmpty(oldImageUrl) && oldImageUrl != category.ImageUrl)
                _imageStorage.DeleteImage(oldImageUrl);

            _unitOfWork.Categories.Update(category);
            await _unitOfWork.SaveChangesAsync();

            var categoryDto = _mapper.Map<CategoryDTO>(category);
            return Result.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category {CategoryId}", dto.Id);
            return Result.Failure<CategoryDTO>(
                "Errore aggiornamento categoria",
                ErrorCode.InternalServerError);
        }
    }

    public async Task<Result> DeleteAsync(int id)
    {
        try
        {
            var category = await _unitOfWork.Categories.GetByIdAsync(id);
            if (category == null)
            {
                return Result.Failure("Categoria non trovata", ErrorCode.NotFound);
            }

            // Check if category has products
            var hasProducts = await _unitOfWork.Products
                .AnyAsync(p => p.CategoryId == id);

            if (hasProducts)
            {
                return Result.Failure("Impossibile eliminare: categoria contiene prodotti",
                    ErrorCode.BadRequest);
            }

            // Clean up image file
            if (!string.IsNullOrEmpty(category.ImageUrl))
                _imageStorage.DeleteImage(category.ImageUrl);

            _unitOfWork.Categories.Delete(category);
            await _unitOfWork.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category {CategoryId}", id);
            return Result.Failure(
                "Errore eliminazione categoria",
                ErrorCode.InternalServerError);
        }
    }

    /// <summary>
    /// Generate a URL-friendly slug from a category name
    /// </summary>
    private static string GenerateSlug(string name)
    {
        var slug = name.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"[\s-]+", "-");
        slug = slug.Trim('-');
        return slug;
    }
}