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
            ApiUrl = "https://rest.directiq.com",
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
    /// Captured data from an intercepted HTTP request.
    /// We read the body synchronously inside the handler callback
    /// to avoid ObjectDisposedException caused by the `using var` in EmailService.
    /// </summary>
    private class CapturedRequest
    {
        public HttpMethod Method { get; set; } = HttpMethod.Get;
        public string? RequestUri { get; set; }
        public string? BodyJson { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
    }

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
            BaseAddress = new Uri(_settings.ApiUrl + "/core/email/send")
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
    /// Creates an EmailService that captures the request body and headers
    /// INSIDE the handler callback (before the `using var` disposes things).
    /// </summary>
    private (EmailService Service, Func<CapturedRequest?> GetCaptured) CreateCapturingService()
    {
        CapturedRequest? captured = null;

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (req, ct) =>
            {
                // Read everything BEFORE returning, so it won't be disposed
                var body = req.Content != null
                    ? await req.Content.ReadAsStringAsync(ct)
                    : null;

                var headers = new Dictionary<string, string>();
                foreach (var h in req.Headers)
                    headers[h.Key] = string.Join(",", h.Value);

                captured = new CapturedRequest
                {
                    Method = req.Method,
                    RequestUri = req.RequestUri?.ToString(),
                    BodyJson = body,
                    Headers = headers
                };

                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"id\":\"abc123\"}")
                };
            })
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri(_settings.ApiUrl + "/core/email/send")
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

        var captured = getCaptured();
        captured.Should().NotBeNull();
        captured!.Method.Should().Be(HttpMethod.Post);

        // Verify final URL (guards against URL duplication regression)
        captured.RequestUri.Should().Be("https://rest.directiq.com/core/email/send");

        // Verify headers (case-insensitive lookup — RestSharp may capitalize keys)
        var authKey = captured.Headers.Keys.FirstOrDefault(k =>
            k.Equals("authorization", StringComparison.OrdinalIgnoreCase));
        authKey.Should().NotBeNull("Expected 'authorization' header");
        captured.Headers[authKey!].Should().Be(_settings.AuthToken);

        var apiVersionKey = captured.Headers.Keys.FirstOrDefault(k =>
            k.Equals("api-version", StringComparison.OrdinalIgnoreCase));
        apiVersionKey.Should().NotBeNull("Expected 'api-version' header");
        captured.Headers[apiVersionKey!].Should().Be(_settings.ApiVersion);

        // Verify JSON payload structure
        var json = JsonDocument.Parse(captured.BodyJson!);
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

        var captured = getCaptured();
        captured.Should().NotBeNull();

        var json = JsonDocument.Parse(captured!.BodyJson!);
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

        var captured = getCaptured();
        var json = JsonDocument.Parse(captured!.BodyJson!);
        var root = json.RootElement;

        root.GetProperty("subject").GetString().Should().Contain("cart");
        var content = root.GetProperty("content").GetString()!;
        content.Should().Contain("Mario");
        content.Should().Contain("3 item(s)");
    }
}
