using AudioStore.Common.DTOs.Email;

namespace AudioStore.Common.Services.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(EmailRequestDTO request);
    Task<bool> SendAbandonedCartEmailAsync(string toEmail, string toName, decimal cartTotal, int itemCount);
    Task<bool> SendOrderConfirmationEmailAsync(string toEmail, string toName, int orderId, decimal total);
}
