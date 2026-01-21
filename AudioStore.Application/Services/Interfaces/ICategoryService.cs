using AudioStore.Application.DTOs.Category;
using AudioStore.Common.Result;

namespace AudioStore.Application.Services.Interfaces;

public interface ICategoryService
{
    Task<Result<IEnumerable<CategoryDTO>>> GetAllAsync(); // For both Admin and users
    Task<Result<CategoryDTO>> GetByIdAsync(int id); // For Admin to show Category details
    Task<Result<CategoryDTO>> CreateAsync(CategoryDTO dto); // For Admin to create new Category
    Task<Result<CategoryDTO>> UpdateAsync(CategoryDTO dto); // For Admin to update the details of category
    Task<Result> DeleteAsync(int id); // For Admin to remove a category. 


}
