using System.Text.Json.Serialization;

namespace CryptoCloudApi.Models.DTOs.Requests;

/// <summary>
/// Request model for creating a CryptoCloud invoice
/// </summary>
public class CreateInvoiceRequest
{
    [JsonPropertyName("shop_id")]
    public string ShopId { get; set; } = string.Empty;

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("currency")]
    public string? Currency { get; set; }

    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    //[JsonPropertyName("email")]
    //public string? Email { get; set; }

    //[JsonPropertyName("add_fields")]
    //public AdditionalFields? AddFields { get; set; }
}

/// <summary>
/// Additional fields for invoice creation
/// </summary>
public class AdditionalFields
{
    [JsonPropertyName("time_to_pay")]
    public TimeToPay? TimeToPay { get; set; }

    [JsonPropertyName("email_to_send")]
    public string? EmailToSend { get; set; }

    [JsonPropertyName("available_currencies")]
    public List<string>? AvailableCurrencies { get; set; }

    [JsonPropertyName("cryptocurrency")]
    public string? Cryptocurrency { get; set; }

    [JsonPropertyName("period")]
    public string? Period { get; set; }
}

/// <summary>
/// Time to pay configuration
/// </summary>
public class TimeToPay
{
    [JsonPropertyName("hours")]
    public int Hours { get; set; } = 24;

    [JsonPropertyName("minutes")]
    public int Minutes { get; set; } = 0;
}
