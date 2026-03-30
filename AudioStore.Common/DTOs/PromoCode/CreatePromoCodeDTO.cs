using AudioStore.Common.Enums;

namespace AudioStore.Common.DTOs.PromoCode;

public class CreatePromoCodeDTO
{
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUsages { get; set; }
    public bool IsActive { get; set; } = true;
}
