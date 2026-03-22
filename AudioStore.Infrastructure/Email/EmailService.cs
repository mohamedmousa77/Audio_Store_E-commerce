using AudioStore.Common.Configuration;
using AudioStore.Common.DTOs.Email;
using AudioStore.Common.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using RestSharp;

namespace AudioStore.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly DirectIqSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private readonly RestClient _client;
    private readonly IHttpClientFactory _httpClientFactory;

    public EmailService(
        IOptions<DirectIqSettings> settings,
        ILogger<EmailService> logger,
        IHttpClientFactory httpClientFactory
        )
    {
        _settings = settings.Value;
        _logger = logger;
        _client = new RestClient(_settings.ApiUrl);
        _httpClientFactory = httpClientFactory;
    }

    public async Task<bool> SendEmailAsync(EmailRequestDTO request)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("DirectIQ");
            using var client = new RestClient(httpClient); // Disposed

            var restRequest = new RestRequest("/v1/email/send", Method.Post);
            restRequest.AddHeader("Authorization", _settings.AuthToken);
            restRequest.AddHeader("Content-Type", "application/json");

            var payload = new
            {
                from = new { email = _settings.FromAddress, name = _settings.FromName },
                to = new[] { new { email = request.ToEmail, name = request.ToName } },
                subject = request.Subject,
                html = request.HtmlBody,
                text = request.PlainTextBody ?? string.Empty
            };

            restRequest.AddJsonBody(payload);

            var response = await _client.ExecuteAsync(restRequest);

            if (response.IsSuccessful)
            {
                _logger.LogInformation(
                    "Email sent successfully to {Email} | Subject: {Subject}",
                    request.ToEmail, request.Subject);
                return true;
            }

            _logger.LogWarning(
                "DirectIQ email failed. Status: {Status} | Response: {Response}",
                response.StatusCode, response.Content);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", request.ToEmail);
            return false;
        }
    }

    public async Task<bool> SendAbandonedCartEmailAsync(
        string toEmail, string toName, decimal cartTotal, int itemCount)
    {
        var html = BuildAbandonedCartHtml(toName, cartTotal, itemCount);

        var request = new EmailRequestDTO
        {
            ToEmail = toEmail,
            ToName = toName,
            Subject = "You left something in your cart! 🎧",
            HtmlBody = html
        };

        return await SendEmailAsync(request);
    }

    public async Task<bool> SendOrderConfirmationEmailAsync(
        string toEmail, string toName, int orderId, decimal total)
    {
        var html = BuildOrderConfirmationHtml(toName, orderId, total);

        var request = new EmailRequestDTO
        {
            ToEmail = toEmail,
            ToName = toName,
            Subject = $"Order #{orderId} Confirmed - Audio Store",
            HtmlBody = html
        };

        return await SendEmailAsync(request);
    }

    // ─── Email Templates ────────────────────────────────────────────────────

    private string BuildAbandonedCartHtml(
        string name, decimal total, int itemCount) =>
            $"""
                <html>
                <body style="font-family: Arial, sans-serif; color: #333;">
                    <div style="max-width: 600px; margin: auto; padding: 24px;">
                        <h2 style="color: #1a1a2e;">Hi {name}, your cart is waiting! 🎧</h2>
                        <p>You have <strong>{itemCount} item(s)</strong> in your cart
                           with a total of <strong>${total:F2}</strong>.</p>
                        <p>Come back and complete your purchase before they sell out!</p>
                        <a href=$"{_settings.AudioStoreUrl}/cart"
                           style="display:inline-block; padding:12px 24px;
                                  background:#6c63ff; color:white;
                                  border-radius:6px; text-decoration:none;">
                            Return to Cart
                        </a>
                        <hr style="margin-top:32px;"/>
                        <p style="font-size:12px; color:#999;">
                            Audio Store — Premium Audio Equipment
                        </p>
                    </div>
                </body>
                </html>
            """;

    private string BuildOrderConfirmationHtml(
        string name, int orderId, decimal total)
        => $"""
            <html>
            <body style="font-family: Arial, sans-serif; color: #333;">
                <div style="max-width: 600px; margin: auto; padding: 24px;">
                    <h2 style="color: #1a1a2e;">Order Confirmed! ✅</h2>
                    <p>Hi <strong>{name}</strong>, your order
                       <strong>#{orderId}</strong> has been placed successfully.</p>
                    <p>Total: <strong>${total:F2}</strong></p>
                    <a href=$"{_settings.AudioStoreUrl}/orders/{orderId}"
                       style="display:inline-block; padding:12px 24px;
                              background:#6c63ff; color:white;
                              border-radius:6px; text-decoration:none;">
                        View Order
                    </a>
                    <hr style="margin-top:32px;"/>
                    <p style="font-size:12px; color:#999;">Audio Store</p>
                </div>
            </body>
            </html>
        """;
}
