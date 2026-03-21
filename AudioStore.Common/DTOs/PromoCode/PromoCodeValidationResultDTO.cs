namespace AudioStore.Common.DTOs.PromoCode;

public class PromoCodeValidationResultDTO
{
    public bool IsValid { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? PromoCodeId { get; set; }

    public static PromoCodeValidationResultDTO Invalid(string message) => new()
    {
        IsValid = false,
        DiscountAmount = 0,
        FinalAmount = 0,
        Message = message
    };

    public static PromoCodeValidationResultDTO Valid(decimal discount, decimal subtotal, int promoId) => new()
    {
        IsValid = true,
        DiscountAmount = discount,
        FinalAmount = subtotal - discount,
        Message = $"Codice applicato! Risparmi {discount:C}",
        PromoCodeId = promoId
    };
}
