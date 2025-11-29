using System.Text.Json.Serialization;

namespace CryptoCloudApi.Models.DTOs.Responses;

/// <summary>
/// Postback notification received from CryptoCloud when payment is completed
/// </summary>
public class PostbackNotification
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("invoice_id")]
    public string InvoiceId { get; set; } = string.Empty;

    [JsonPropertyName("amount_crypto")]
    public decimal AmountCrypto { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("invoice_info")]
    public PostbackInvoiceInfo? InvoiceInfo { get; set; }
}

/// <summary>
/// Detailed invoice information included in postback
/// </summary>
public class PostbackInvoiceInfo
{
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; }

    [JsonPropertyName("created")]
    public string Created { get; set; }

    [JsonPropertyName("address")]
    public string Address { get; set; }

    [JsonPropertyName("currency")]
    public CurrencyInfo? Currency { get; set; }

    [JsonPropertyName("date_finished")]
    public string DateFinished { get; set; }

    [JsonPropertyName("expiry_date")]
    public string ExpiryDate { get; set; }

    [JsonPropertyName("side_commission")]
    public string SideCommission { get; set; }

    [JsonPropertyName("type_payments")]
    public string TypePayments { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("invoice_status")]
    public string InvoiceStatus { get; set; }

    [JsonPropertyName("is_email_required")]
    public bool IsEmailRequired { get; set; }

    [JsonPropertyName("project")]
    public ProjectInfo? Project { get; set; }

    [JsonPropertyName("tx_list")]
    public List<string>? TxList { get; set; }

    [JsonPropertyName("amount_in_crypto")]
    public decimal AmountInCrypto { get; set; }

    [JsonPropertyName("amount_in_fiat")]
    public decimal AmountInFiat { get; set; }

    [JsonPropertyName("amount_usd")]
    public decimal AmountUsd { get; set; }

    [JsonPropertyName("amount_to_pay")]
    public decimal AmountToPay { get; set; }

    [JsonPropertyName("amount_to_pay_usd")]
    public decimal AmountToPayUsd { get; set; }

    [JsonPropertyName("amount_paid")]
    public decimal AmountPaid { get; set; }

    [JsonPropertyName("amount_paid_usd")]
    public decimal AmountPaidUsd { get; set; }

    [JsonPropertyName("fee")]
    public decimal Fee { get; set; }

    [JsonPropertyName("fee_usd")]
    public decimal FeeUsd { get; set; }

    [JsonPropertyName("service_fee")]
    public decimal ServiceFee { get; set; }

    [JsonPropertyName("service_fee_usd")]
    public decimal ServiceFeeUsd { get; set; }

    [JsonPropertyName("received")]
    public decimal Received { get; set; }

    [JsonPropertyName("received_usd")]
    public decimal ReceivedUsd { get; set; }

    [JsonPropertyName("to_surcharge")]
    public decimal ToSurcharge { get; set; }

    [JsonPropertyName("to_surcharge_usd")]
    public decimal ToSurchargeUsd { get; set; }

    [JsonPropertyName("total_rub")]
    public decimal TotalRub { get; set; }

    [JsonPropertyName("step")]
    public string? Step { get; set; }

    [JsonPropertyName("test_mode")]
    public bool? TestMode { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("aml_enabled")]
    public bool? AmlEnabled { get; set; }

    [JsonPropertyName("aml_side")]
    public string? AmlSide { get; set; }

    [JsonPropertyName("aml_checks")]
    public object? AmlChecks { get; set; }

    [JsonPropertyName("links_invoice")]
    public string? LinksInvoice { get; set; }
}
