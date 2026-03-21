using AudioStore.Common.Enums;

namespace AudioStore.Common.DTOs.PromoCode;

public class PromoCodeResponseDTO
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Quanti utenti ce l'hanno assegnato e quanti lo hanno usato
    public int TotalAssigned { get; set; }
    public int TotalUsed { get; set; }
}
