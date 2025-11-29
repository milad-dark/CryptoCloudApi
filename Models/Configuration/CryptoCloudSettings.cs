namespace CryptoCloudApi.Models.Configuration;

/// <summary>
/// Configuration settings for CryptoCloud API integration
/// </summary>
public class CryptoCloudSettings
{
    public const string SectionName = "CryptoCloud";

    /// <summary>
    /// Your CryptoCloud API Key for authorization
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Your CryptoCloud Shop ID
    /// </summary>
    public string ShopId { get; set; } = string.Empty;

    /// <summary>
    /// JWT Secret Key for validating postback signatures
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for CryptoCloud API
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.cryptocloud.plus";

    /// <summary>
    /// Default currency for invoices (USD, EUR, RUB, etc.)
    /// </summary>
    public string DefaultCurrency { get; set; } = "USD";

    /// <summary>
    /// Enable test mode for development
    /// </summary>
    public bool TestMode { get; set; } = false;

    /// <summary>
    /// How often to check invoice status (in seconds)
    /// </summary>
    public int StatusCheckIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// How long to monitor pending invoices (in hours)
    /// </summary>
    public int MonitoringPeriodHours { get; set; } = 24;
}
