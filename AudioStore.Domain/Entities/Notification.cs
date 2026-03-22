using AudioStore.Common.Constants;
using AudioStore.Common.Enums;

namespace AudioStore.Domain.Entities;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }


    // Navigation
    public virtual User? User { get; set; }
}
