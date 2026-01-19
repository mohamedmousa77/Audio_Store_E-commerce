namespace AudioStore.Application.DTOs.Cart;

public record GetCartDTO
{
    public int? UserId { get; init; }
    public string? SessionId { get; init; }
}
