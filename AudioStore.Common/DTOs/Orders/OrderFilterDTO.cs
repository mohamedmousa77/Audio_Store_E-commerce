using AudioStore.Common.Enums;

namespace AudioStore.Common.DTOs.Orders;

public record OrderFilterDTO
{
    public int? UserId { get; init; }
    public OrderStatus? Status { get; init; }
    public string? CustomerSearch { get; init; } // Nome o email
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
