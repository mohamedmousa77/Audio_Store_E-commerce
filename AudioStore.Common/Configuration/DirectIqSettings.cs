namespace AudioStore.Common.Configuration;

public class DirectIqSettings
{
    public const string SectionName = "DirectIQ";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.directiq.com";
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public int AbandonedCartMinutes { get; set; } = 30; // Trigger
}
