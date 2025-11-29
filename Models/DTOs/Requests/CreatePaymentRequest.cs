namespace CryptoCloudApi.Models.DTOs.Requests;

/// <summary>
/// Request model for creating a payment
/// </summary>
public class CreatePaymentRequest
{
    public string OrderId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Email { get; set; }
    public string? Cryptocurrency { get; set; }
    public List<string>? AvailableCurrencies { get; set; }
    public int? TimeToPayHours { get; set; }
    public int? TimeToPayMinutes { get; set; }
    public string? Metadata { get; set; }
}
