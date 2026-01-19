using AudioStore.Application.DTOs.Category;
using AudioStore.Common.Result;

namespace AudioStore.Application.Services.Interfaces;

public interface ICategoryService
{
    Task<Result<IEnumerable<CategoryDTO>>> GetAllAsync();
    Task<Result<CategoryDTO>> GetByIdAsync(int id);
    Task<Result<CategoryDTO>> CreateAsync(CategoryDTO dto);
    Task<Result<CategoryDTO>> UpdateAsync(CategoryDTO dto);
    Task<Result> DeleteAsync(int id);


}
