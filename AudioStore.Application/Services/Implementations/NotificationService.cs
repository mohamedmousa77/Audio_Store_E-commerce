using AudioStore.Common;
using AudioStore.Common.Constants;
using AudioStore.Common.DTOs.Notification;
using AudioStore.Common.DTOs.Orders;
using AudioStore.Common.Enums;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace AudioStore.Application.Services.Implementations;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
       IUnitOfWork unitOfWork,
       IMapper mapper,
       ILogger<NotificationService> logger)
    {
        this._unitOfWork = unitOfWork;
        this._mapper = mapper;
        this._logger = logger;
    }
    public async Task<Result<IEnumerable<NotificationDTO>>> GetUserNotificationsAsync(int userId)
    {
        try
        {
            var notifications = await _unitOfWork.Notifications.GetByUserIdAsync(userId);
            var dtos = _mapper.Map<IEnumerable<NotificationDTO>>(notifications);
            return Result.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications for user {UserId}", userId);
            return Result.Failure < IEnumerable < NotificationDTO >> ("Error retrieving notifications",
                    ErrorCode.InternalServerError);
        }
    }

    public async Task<Result<int>> GetUnreadCountAsync(int userId)
    {
        try
        {
            var count = await _unitOfWork.Notifications.GetUnreadCountAsync(userId);
            return Result.Success<int>(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            return Result.Failure<int>("Error", ErrorCode.InternalServerError);
        }
    }

    public async Task<Result> MarkAsReadAsync(int notificationId, int requestingUserId)
    {
        try
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);

            if (notification == null)
                return Result.Failure("Notification not found", ErrorCode.NotFound);

            // Ownership check
            if (notification.UserId != requestingUserId)
            {
                _logger.LogWarning(
                    "Unauthorized MarkAsRead attempt: User {RequestingUserId} tried to access Notification {NotificationId} owned by User {OwnerId}",
                    requestingUserId, notificationId, notification.UserId);

                // Rispondi NotFound, non Forbidden — non confermare che l'ID esiste
                return Result.Failure("Notification not found", ErrorCode.NotFound);
            }
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            notification.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Notifications.MarkAsReadAsync(notificationId);
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {Id} as read", notificationId);
            return Result.Failure("Error", ErrorCode.InternalServerError);
        }
    }

    public async Task<Result> MarkAllAsReadAsync(int userId)
    {
        try
        {
            await _unitOfWork.Notifications.MarkAllAsReadAsync(userId);
            await _unitOfWork.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read for user {UserId}", userId);
            return Result.Failure("Error", ErrorCode.InternalServerError);
        }
    }

    public async Task CreateNotificationAsync(
        int userId, string title, string message, NotificationType type)
    {
        try
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Notification created for user {UserId} | Type: {Type}", userId, type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating notification for user {UserId}", userId);
        }
    }
}
