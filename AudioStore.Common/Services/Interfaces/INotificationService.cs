using AudioStore.Common.DTOs.Notification;

namespace AudioStore.Common.Services.Interfaces;

public interface INotificationService
{
    Task<Result<IEnumerable<NotificationDTO>>> GetUserNotificationsAsync(int userId);
    Task<Result<int>> GetUnreadCountAsync(int userId);
    Task<Result> MarkAsReadAsync(int notificationId);
    Task<Result> MarkAllAsReadAsync(int userId);
    Task CreateNotificationAsync(int userId, string title, string message, string type);
}
