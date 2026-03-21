using AudioStore.Common.Enums;

namespace AudioStore.Domain.Entities;

public class PromoCode : BaseEntity
{
    public string Code { get; set; } = string.Empty; // es. "SUMMER20"
    public DiscountType DiscountType { get; set; }   // Percentage | FixedAmount
    public decimal DiscountValue { get; set; }        // es. 20 (%) o 10 (€)
    public decimal? MinOrderAmount { get; set; }      // importo minimo ordine
    public int? MaxUsages { get; set; }               // null = illimitato
    public int CurrentUsages { get; set; } = 0;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    // navigations
    public virtual ICollection<UserPromoCode> UserPromoCodes { get; set; } = new List<UserPromoCode>();
}
