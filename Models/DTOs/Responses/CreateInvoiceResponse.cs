using System.Text.Json.Serialization;

namespace CryptoCloudApi.Models.DTOs.Responses;

/// <summary>
/// Response model from CryptoCloud invoice creation
/// </summary>
public class CreateInvoiceResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public InvoiceResult? Result { get; set; }
}

/// <summary>
/// Invoice result data
/// </summary>
public class InvoiceResult
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    [JsonPropertyName("created")]
    public string Created { get; set; } = string.Empty;

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

    [JsonPropertyName("amount_in_fiat")]
    public decimal? AmountInFiat { get; set; }

    [JsonPropertyName("fee")]
    public decimal Fee { get; set; }

    [JsonPropertyName("fee_usd")]
    public decimal FeeUsd { get; set; }

    [JsonPropertyName("service_fee")]
    public decimal ServiceFee { get; set; }

    [JsonPropertyName("service_fee_usd")]
    public decimal ServiceFeeUsd { get; set; }

    [JsonPropertyName("type_payments")]
    public string TypePayments { get; set; } = string.Empty;

    [JsonPropertyName("fiat_currency")]
    public string FiatCurrency { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("is_email_required")]
    public bool IsEmailRequired { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;

    [JsonPropertyName("invoice_id")]
    public string? InvoiceId { get; set; }

    [JsonPropertyName("currency")]
    public CurrencyInfo? Currency { get; set; }

    [JsonPropertyName("project")]
    public ProjectInfo? Project { get; set; }

    [JsonPropertyName("test_mode")]
    public bool TestMode { get; set; }
}

/// <summary>
/// Currency information
/// </summary>
public class CurrencyInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("fullcode")]
    public string FullCode { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("is_email_required")]
    public bool IsEmailRequired { get; set; }

    [JsonPropertyName("stablecoin")]
    public bool Stablecoin { get; set; }

    [JsonPropertyName("icon_base")]
    public string? IconBase { get; set; }

    [JsonPropertyName("icon_network")]
    public string? IconNetwork { get; set; }

    [JsonPropertyName("network")]
    public NetworkInfo? Network { get; set; }
}

/// <summary>
/// Network information
/// </summary>
public class NetworkInfo
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("fullname")]
    public string FullName { get; set; } = string.Empty;

    [JsonPropertyName("icon")]
    public string? Icon { get; set; }
}

/// <summary>
/// Project information
/// </summary>
public class ProjectInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("fail")]
    public string? Fail { get; set; }

    [JsonPropertyName("success")]
    public string? Success { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }
}
