namespace AudioStore.Application.DTOs;

internal class CategoryDTO
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
}
