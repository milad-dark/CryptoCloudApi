using System.Text.Json.Serialization;

namespace CryptoCloudApi.Models.DTOs.Responses;

/// <summary>
/// Response model for invoice information query
/// </summary>
public class InvoiceInfoResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public List<InvoiceInfoResult> Result { get; set; } = new();
}

/// <summary>
/// Detailed invoice information
/// </summary>
public class InvoiceInfoResult
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("expiry_date")]
    public string? ExpiryDate { get; set; }

    [JsonPropertyName("side_commission")]
    public string SideCommission { get; set; } = string.Empty;

    [JsonPropertyName("side_commission_cc")]
    public string? SideCommissionCc { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("amount_usd")]
    public decimal AmountUsd { get; set; }

    [JsonPropertyName("received")]
    public decimal Received { get; set; }

    [JsonPropertyName("received_usd")]
    public decimal ReceivedUsd { get; set; }

    [JsonPropertyName("fee")]
    public decimal Fee { get; set; }

    [JsonPropertyName("fee_usd")]
    public decimal FeeUsd { get; set; }

    [JsonPropertyName("service_fee")]
    public decimal ServiceFee { get; set; }

    [JsonPropertyName("service_fee_usd")]
    public decimal ServiceFeeUsd { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    [JsonPropertyName("currency")]
    public CurrencyInfo? Currency { get; set; }

    [JsonPropertyName("project")]
    public ProjectInfo? Project { get; set; }

    [JsonPropertyName("test_mode")]
    public bool TestMode { get; set; }

    [JsonPropertyName("tx_list")]
    public List<string>? TxList { get; set; }
}
