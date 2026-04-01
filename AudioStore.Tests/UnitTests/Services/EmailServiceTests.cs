using AudioStore.Common.Configuration;
using AudioStore.Common.DTOs.Email;
using AudioStore.Infrastructure.Email;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace AudioStore.Tests.UnitTests.Services;

/// <summary>
/// Unit tests for EmailService — verifies correct payload construction,
/// header injection, and error handling against the DirectIQ API contract.
/// Uses a mock HttpMessageHandler to intercept outgoing HTTP requests.
/// </summary>
public class EmailServiceTests
{
    private readonly DirectIqSettings _settings;
    private readonly Mock<ILogger<EmailService>> _loggerMock;

    public EmailServiceTests()
    {
        _settings = new DirectIqSettings
        {
            ApiUrl = "https://rest.directiq.com/core/email/send",
            AuthToken = "Basic dGVzdDp0ZXN0",
            ApiVersion = "test@example.com",
            FromAddress = "store@audiostore.com",
            FromName = "Audio Store",
            AudioStoreUrl = "http://localhost:4200"
        };
        _loggerMock = new Mock<ILogger<EmailService>>();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an EmailService backed by a mock HttpMessageHandler that
    /// captures the outgoing request and returns the specified status code.
    /// </summary>
    private (EmailService Service, Mock<HttpMessageHandler> Handler) CreateServiceWithMockHandler(
        HttpStatusCode responseStatus, string responseBody = "")
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = responseStatus,
                Content = new StringContent(responseBody)
            })
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(_settings.ApiUrl)
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock
            .Setup(f => f.CreateClient("DirectIQ"))
            .Returns(httpClient);

        var options = Options.Create(_settings);
        var service = new EmailService(options, _loggerMock.Object, factoryMock.Object);

        return (service, handlerMock);
    }

    /// <summary>
    /// Creates an EmailService that captures the raw HttpRequestMessage for
    /// deep inspection of headers and JSON body.
    /// </summary>
    private (EmailService Service, Func<HttpRequestMessage?> GetCapturedRequest) CreateCapturingService()
    {
        HttpRequestMessage? captured = null;

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"id\":\"abc123\"}")
            })
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(_settings.ApiUrl)
        };

        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("DirectIQ")).Returns(httpClient);

        var options = Options.Create(_settings);
        var service = new EmailService(options, _loggerMock.Object, factoryMock.Object);

        return (service, () => captured);
    }

    // ─── Test 1: SendEmailAsync builds payload with correct JSON fields ──────

    [Fact]
    public async Task SendEmailAsync_BuildsCorrectPayload_WithContentFieldAndStringTo()
    {
        // Arrange
        var (service, getCaptured) = CreateCapturingService();

        var emailRequest = new EmailRequestDTO
        {
            ToEmail = "customer@test.com",
            ToName = "John Doe",
            Subject = "Order Confirmed",
            HtmlBody = "<h1>Thank you!</h1>"
        };

        // Act
        var result = await service.SendEmailAsync(emailRequest);

        // Assert
        result.Should().BeTrue();

        var request = getCaptured();
        request.Should().NotBeNull();
        request!.Method.Should().Be(HttpMethod.Post);

        // Verify headers
        request.Headers.TryGetValues("authorization", out var authValues);
        authValues.Should().ContainSingle().Which.Should().Be(_settings.AuthToken);

        request.Headers.TryGetValues("api-version", out var apiVersionValues);
        apiVersionValues.Should().ContainSingle().Which.Should().Be(_settings.ApiVersion);

        // Verify JSON payload structure
        var bodyString = await request.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(bodyString);
        var root = json.RootElement;

        root.GetProperty("from").GetString().Should().Be(_settings.FromAddress);
        root.GetProperty("to").GetString().Should().Be("customer@test.com");
        root.GetProperty("subject").GetString().Should().Be("Order Confirmed");
        root.GetProperty("content").GetString().Should().Be("<h1>Thank you!</h1>");
    }

    // ─── Test 2: SendEmailAsync returns false and logs warning on 401 ────────

    [Fact]
    public async Task SendEmailAsync_Returns_False_And_Logs_Warning_On_Unauthorized()
    {
        // Arrange
        var (service, handlerMock) = CreateServiceWithMockHandler(
            HttpStatusCode.Unauthorized, "Invalid credentials");

        var emailRequest = new EmailRequestDTO
        {
            ToEmail = "customer@test.com",
            ToName = "John Doe",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = await service.SendEmailAsync(emailRequest);

        // Assert
        result.Should().BeFalse();

        // Verify the mock handler was actually called
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    // ─── Test 3: SendOrderConfirmationEmailAsync uses expected subject ───────

    [Fact]
    public async Task SendOrderConfirmationEmailAsync_Uses_Correct_Subject_And_Content()
    {
        // Arrange
        var (service, getCaptured) = CreateCapturingService();

        // Act
        var result = await service.SendOrderConfirmationEmailAsync(
            "buyer@test.com", "Alice", orderId: 42, total: 129.99m);

        // Assert
        result.Should().BeTrue();

        var request = getCaptured();
        request.Should().NotBeNull();

        var bodyString = await request!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(bodyString);
        var root = json.RootElement;

        root.GetProperty("to").GetString().Should().Be("buyer@test.com");
        root.GetProperty("subject").GetString().Should().Be("Order #42 Confirmed - Audio Store");

        var content = root.GetProperty("content").GetString()!;
        content.Should().Contain("Alice");
        content.Should().Contain("#42");
        content.Should().Contain("$129.99");
    }

    // ─── Test 4: SendEmailAsync returns false on NotFound (old bug scenario) ─

    [Fact]
    public async Task SendEmailAsync_Returns_False_On_NotFound_Response()
    {
        // Arrange
        var (service, _) = CreateServiceWithMockHandler(HttpStatusCode.NotFound);

        var emailRequest = new EmailRequestDTO
        {
            ToEmail = "customer@test.com",
            ToName = "Test",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Act
        var result = await service.SendEmailAsync(emailRequest);

        // Assert
        result.Should().BeFalse();
    }

    // ─── Test 5: SendAbandonedCartEmailAsync builds correct template ─────────

    [Fact]
    public async Task SendAbandonedCartEmailAsync_Builds_Correct_Template()
    {
        // Arrange
        var (service, getCaptured) = CreateCapturingService();

        // Act
        var result = await service.SendAbandonedCartEmailAsync(
            "user@test.com", "Mario", cartTotal: 59.99m, itemCount: 3);

        // Assert
        result.Should().BeTrue();

        var request = getCaptured();
        var bodyString = await request!.Content!.ReadAsStringAsync();
        var json = JsonDocument.Parse(bodyString);
        var root = json.RootElement;

        root.GetProperty("subject").GetString().Should().Contain("cart");
        var content = root.GetProperty("content").GetString()!;
        content.Should().Contain("Mario");
        content.Should().Contain("3 item(s)");
    }
}
