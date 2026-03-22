namespace AudioStore.Common.Configuration;

public class DirectIqSettings
{
    public const string SectionName = "DirectIQ";
    public string ApiUrl { get; set; } = string.Empty;
    public string AuthToken { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public int AbandonedCartMinutes { get; set; } = 30; // Trigger
    public string AudioStoreUrl { get; set; } = string.Empty;

    /// <summary>
    /// Minimum hours between two abandoned cart emails to the same user.
    /// Default: 24h → massimo 1 email al giorno per carrello abbandonato.
    /// </summary>
    public int AbandonedCartEmailCooldownHours { get; set; } = 24;
}