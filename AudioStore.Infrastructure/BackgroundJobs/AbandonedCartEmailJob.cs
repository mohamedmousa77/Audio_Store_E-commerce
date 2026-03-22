using AudioStore.Common.Configuration;
using AudioStore.Common.Constants;
using AudioStore.Common.Enums;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AudioStore.Infrastructure.BackgroundJobs;

public class AbandonedCartEmailJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AbandonedCartEmailJob> _logger;
    private readonly DirectIqSettings _settings;

    // Run every 15 minutes
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    public AbandonedCartEmailJob(
        IServiceScopeFactory scopeFactory,
        ILogger<AbandonedCartEmailJob> logger,
        IOptions<DirectIqSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AbandonedCartEmailJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAbandonedCartsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in AbandonedCartEmailJob");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task ProcessAbandonedCartsAsync()
    {
        using var scope = _scopeFactory.CreateScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        // Threshold: carts not updated in X minutes (configured in settings)
        var now = DateTime.UtcNow;
        var threshold = DateTime.UtcNow.AddMinutes(-_settings.AbandonedCartMinutes);

        var emailCooldown = now.AddHours(-_settings.AbandonedCartEmailCooldownHours);
        // Fetch all active authenticated-user carts with items,
        // updated before the threshold (i.e., user hasn't touched it)
        var abandonedCarts = await unitOfWork.Carts
            .GetAbandonedCartsAsync(threshold, emailCooldown);

        _logger.LogInformation(
            "AbandonedCartJob: found {Count} abandoned carts to process.",
            abandonedCarts.Count());

        foreach (var cart in abandonedCarts)
        {
            // Only process carts belonging to authenticated users (not guest)
            if (!cart.UserId.HasValue || cart.User?.Email == null)
                continue;

            var user = cart.User;
            var itemCount = cart.CartItems.Count;
            var total = cart.TotalAmount;

            // Send email via DirectIQ
            var emailSent = await emailService.SendAbandonedCartEmailAsync(
                toEmail: user.Email,
                toName: user.FullName,
                cartTotal: total,
                itemCount: itemCount);

            if (emailSent)
            {
                // Stamp the timestamp IMMEDIATELY to prevent re-sending
                cart.LastAbandonedCartEmailSentAt = now;
                cart.UpdatedAt = now;
                unitOfWork.Carts.Update(cart);
                await unitOfWork.SaveChangesAsync();

                // Create in-app notification
                await notificationService.CreateNotificationAsync(
                    userId: user.Id,
                    title: "You left items in your cart!",
                    message: $"You have {itemCount} item(s) worth ${total:F2} waiting in your cart.",
                    type: NotificationType.AbandonedCart);

                _logger.LogInformation(
                    "Abandoned cart email sent to user {UserId} ({Email})",
                    user.Id, user.Email);
            }
        }
    }

}
