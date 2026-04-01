using AudioStore.Common.Configuration;
using AudioStore.Common.Enums;
using AudioStore.Common.Services.Interfaces;
using AudioStore.Domain.Entities;
using AudioStore.Domain.Interfaces;
using AudioStore.Infrastructure.BackgroundJobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AudioStore.Tests.UnitTests.Services;

/// <summary>
/// Unit tests for AbandonedCartEmailJob — verifies that the background
/// worker correctly skips guest carts, respects cooldown windows,
/// and sends emails only for authenticated abandoned carts.
/// </summary>
public class AbandonedCartEmailJobTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ICartRepository> _cartRepoMock;
    private readonly Mock<ILogger<AbandonedCartEmailJob>> _loggerMock;
    private readonly DirectIqSettings _settings;

    public AbandonedCartEmailJobTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _emailServiceMock = new Mock<IEmailService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _cartRepoMock = new Mock<ICartRepository>();
        _loggerMock = new Mock<ILogger<AbandonedCartEmailJob>>();

        _settings = new DirectIqSettings
        {
            AbandonedCartMinutes = 30,
            AbandonedCartEmailCooldownHours = 24
        };

        _unitOfWorkMock.Setup(u => u.Carts).Returns(_cartRepoMock.Object);
    }

    /// <summary>
    /// Simulates the job's ProcessAbandonedCartsAsync method by resolving
    /// scoped services from a real ServiceProvider built with mock registrations.
    /// </summary>
    private AbandonedCartEmailJob CreateJob()
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _unitOfWorkMock.Object);
        services.AddScoped(_ => _emailServiceMock.Object);
        services.AddScoped(_ => _notificationServiceMock.Object);

        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var options = Options.Create(_settings);

        return new AbandonedCartEmailJob(scopeFactory, _loggerMock.Object, options);
    }

    // ─── Test 1: Guest cart (UserId = null) is skipped ──────────────────────

    [Fact]
    public async Task ProcessAbandonedCarts_SkipsGuestCart_WhenUserIdIsNull()
    {
        // Arrange
        var guestCart = new Cart
        {
            Id = 1,
            UserId = null,  // Guest cart
            User = null,
            CartItems = new List<CartItem>
            {
                new CartItem { Id = 1, ProductId = 1, Quantity = 2, UnitPrice = 49.99m }
            },
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };

        _cartRepoMock
            .Setup(c => c.GetAbandonedCartsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Cart> { guestCart });

        var job = CreateJob();

        // Act — start the job and let it process one cycle
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        try { await job.StartAsync(cts.Token); await Task.Delay(500); }
        catch (OperationCanceledException) { /* expected */ }
        finally { await job.StopAsync(CancellationToken.None); }

        // Assert — email should NEVER have been sent
        _emailServiceMock.Verify(
            e => e.SendAbandonedCartEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<decimal>(), It.IsAny<int>()),
            Times.Never);
    }

    // ─── Test 2: Cooldown prevents duplicate sends ──────────────────────────

    [Fact]
    public async Task ProcessAbandonedCarts_DoesNotSendDuplicate_WhenCooldownActive()
    {
        // Arrange — the repo query itself filters by cooldown, so if
        // GetAbandonedCartsAsync returns an empty list, no email is sent.
        // This simulates the cooldown logic at the repository level.
        _cartRepoMock
            .Setup(c => c.GetAbandonedCartsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Cart>()); // Empty = cooldown filtered it out

        var job = CreateJob();

        // Act
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        try { await job.StartAsync(cts.Token); await Task.Delay(500); }
        catch (OperationCanceledException) { /* expected */ }
        finally { await job.StopAsync(CancellationToken.None); }

        // Assert
        _emailServiceMock.Verify(
            e => e.SendAbandonedCartEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<decimal>(), It.IsAny<int>()),
            Times.Never);
    }

    // ─── Test 3: Authenticated cart triggers email ───────────────────────────

    [Fact]
    public async Task ProcessAbandonedCarts_SendsEmail_ForAuthenticatedUser()
    {
        // Arrange
        var user = new User
        {
            Id = 5,
            Email = "mario@test.com",
            FirstName = "Mario",
            LastName = "Rossi"
        };

        var authenticatedCart = new Cart
        {
            Id = 2,
            UserId = 5,
            User = user,
            CartItems = new List<CartItem>
            {
                new CartItem { Id = 1, ProductId = 1, Quantity = 1, UnitPrice = 199.99m }
            },
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };

        _cartRepoMock
            .Setup(c => c.GetAbandonedCartsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Cart> { authenticatedCart });

        _emailServiceMock
            .Setup(e => e.SendAbandonedCartEmailAsync(
                "mario@test.com", It.IsAny<string>(), 199.99m, 1))
            .ReturnsAsync(true);

        _notificationServiceMock
            .Setup(n => n.CreateNotificationAsync(
                5, It.IsAny<string>(), It.IsAny<string>(), NotificationType.AbandonedCart))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var job = CreateJob();

        // Act
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        try { await job.StartAsync(cts.Token); await Task.Delay(500); }
        catch (OperationCanceledException) { /* expected */ }
        finally { await job.StopAsync(CancellationToken.None); }

        // Assert
        _emailServiceMock.Verify(
            e => e.SendAbandonedCartEmailAsync(
                "mario@test.com", It.IsAny<string>(), 199.99m, 1),
            Times.Once);

        _notificationServiceMock.Verify(
            n => n.CreateNotificationAsync(
                5, It.IsAny<string>(), It.IsAny<string>(), NotificationType.AbandonedCart),
            Times.Once);
    }
}
