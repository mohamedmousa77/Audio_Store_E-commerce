namespace AudioStore.Domain.Entities;

public class UserPromoCode
{
    public int UserId { get; set; }
    public int PromoCodeId { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
    public virtual PromoCode PromoCode { get; set; } = null!;
}

