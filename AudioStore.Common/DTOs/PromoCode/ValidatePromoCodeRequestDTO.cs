namespace AudioStore.Common.DTOs.PromoCode;

public class ValidatePromoCodeRequestDTO
{
    public string Code { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
}
