using AudioStore.Common.Enums;

namespace AudioStore.Common.DTOs.PromoCode;

/// <summary>
/// Usato dall'admin per creare un PromoCode e assegnarlo
/// subito a un cliente specifico in un'unica operazione.
/// </summary>
public class CreateAndAssignPromoCodeDTO
{
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public int? MaxUsages { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// ID dell'utente a cui assegnare il PromoCode al momento della creazione.
    /// </summary>
    public int UserId { get; set; }
}
