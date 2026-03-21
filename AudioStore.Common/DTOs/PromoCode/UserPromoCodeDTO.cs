namespace AudioStore.Common.DTOs.PromoCode;

public class UserPromoCodeDTO
{
    public int PromoCodeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DiscountValue { get; set; } = string.Empty; // es. "20%" o "10€"
    public DateTime? ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime AssignedAt { get; set; }
}
