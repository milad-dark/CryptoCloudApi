using CryptoCloudApi.Models.DTOs.Requests;
using CryptoCloudApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoCloudApi.Controllers;

/// <summary>
/// Endpoints for managing cryptocurrency payment invoices
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Payment Management")]
public class PaymentController(
    InvoiceManagementService invoiceService,
    ILogger<PaymentController> logger) : ControllerBase
{

    /// <summary>
    /// Create a new payment invoice
    /// </summary>
    /// <param name="request">Invoice creation request with amount, order ID, and optional payment preferences</param>
    /// <returns>Created invoice details including payment link</returns>
    /// <remarks>
    /// Sample request:
    /// 
    ///     POST /api/payment/create
    ///     {
    ///        "orderId": "ORDER-12345",
    ///        "amount": 100,
    ///        "currency": "USD",
    ///        "email": "customer@example.com",
    ///        "cryptocurrency": "USDT_TRC20",
    ///        "timeToPayHours": 24
    ///     }
    /// 
    /// </remarks>
    /// <response code="200">Invoice created successfully</response>
    /// <response code="400">Invalid request parameters</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateInvoice([FromBody] CreatePaymentRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.OrderId))
            {
                return BadRequest(new { message = "OrderId is required" });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new { message = "Amount must be greater than 0" });
            }

            var additionalFields = new AdditionalFields();

            if (request.AvailableCurrencies?.Any() == true)
            {
                additionalFields.AvailableCurrencies = request.AvailableCurrencies;
            }

            if (!string.IsNullOrEmpty(request.Cryptocurrency))
            {
                additionalFields.Cryptocurrency = request.Cryptocurrency;
            }

            if (request.TimeToPayHours.HasValue || request.TimeToPayMinutes.HasValue)
            {
                additionalFields.TimeToPay = new TimeToPay
                {
                    Hours = request.TimeToPayHours ?? 24,
                    Minutes = request.TimeToPayMinutes ?? 0
                };
            }

            var invoice = await invoiceService.CreateInvoiceAsync(
                request.OrderId,
                request.Amount,
                request.Currency,
                request.Email,
                additionalFields,
                request.Metadata);

            if (invoice == null)
            {
                return StatusCode(500, new { message = "Failed to create invoice" });
            }

            return Ok(new
            {
                success = true,
                invoice_uuid = invoice.InvoiceUuid,
                payment_link = invoice.PaymentLink,
                payment_address = invoice.PaymentAddress,
                amount = invoice.Amount,
                amount_usd = invoice.AmountUsd,
                currency = invoice.Currency,
                crypto_currency = invoice.CryptoCurrency,
                status = invoice.Status,
                expires_at = invoice.ExpiryDate,
                created_at = invoice.CreatedAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating invoice");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get invoice details by UUID
    /// </summary>
    /// <param name="invoiceUuid">Invoice UUID (with or without INV- prefix, e.g., "INV-XXXXXXXX" or "XXXXXXXX")</param>
    /// <returns>Complete invoice details including transaction history</returns>
    /// <response code="200">Invoice found and returned</response>
    /// <response code="404">Invoice not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{invoiceUuid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInvoice(string invoiceUuid)
    {
        try
        {
            // Normalize UUID
            if (!invoiceUuid.StartsWith("INV-"))
            {
                invoiceUuid = $"INV-{invoiceUuid}";
            }

            var invoice = await invoiceService.GetInvoiceAsync(invoiceUuid);

            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found" });
            }

            return Ok(new
            {
                invoice_uuid = invoice.InvoiceUuid,
                order_id = invoice.OrderId,
                payment_link = invoice.PaymentLink,
                payment_address = invoice.PaymentAddress,
                amount = invoice.Amount,
                amount_usd = invoice.AmountUsd,
                received_amount = invoice.ReceivedAmount,
                currency = invoice.Currency,
                crypto_currency = invoice.CryptoCurrency,
                status = invoice.Status,
                customer_email = invoice.CustomerEmail,
                fee = invoice.Fee,
                service_fee = invoice.ServiceFee,
                test_mode = invoice.TestMode,
                created_at = invoice.CreatedAt,
                expires_at = invoice.ExpiryDate,
                paid_at = invoice.PaidAt,
                last_status_check = invoice.LastStatusCheck,
                transactions = invoice.Transactions.Select(t => new
                {
                    transaction_hash = t.TransactionHash,
                    amount = t.Amount,
                    currency = t.Currency,
                    detected_at = t.DetectedAt,
                    confirmations = t.Confirmations
                })
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting invoice {InvoiceUuid}", invoiceUuid);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get invoice by your internal order ID
    /// </summary>
    /// <param name="orderId">Your internal order identifier</param>
    /// <returns>Invoice details for the specified order</returns>
    /// <response code="200">Invoice found and returned</response>
    /// <response code="404">No invoice found for this order</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("order/{orderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetInvoiceByOrderId(string orderId)
    {
        try
        {
            var invoice = await invoiceService.GetInvoiceByOrderIdAsync(orderId);

            if (invoice == null)
            {
                return NotFound(new { message = "Invoice not found for this order" });
            }

            return Ok(new
            {
                invoice_uuid = invoice.InvoiceUuid,
                order_id = invoice.OrderId,
                payment_link = invoice.PaymentLink,
                payment_address = invoice.PaymentAddress,
                amount = invoice.Amount,
                amount_usd = invoice.AmountUsd,
                received_amount = invoice.ReceivedAmount,
                currency = invoice.Currency,
                crypto_currency = invoice.CryptoCurrency,
                status = invoice.Status,
                customer_email = invoice.CustomerEmail,
                fee = invoice.Fee,
                service_fee = invoice.ServiceFee,
                test_mode = invoice.TestMode,
                created_at = invoice.CreatedAt,
                expires_at = invoice.ExpiryDate,
                paid_at = invoice.PaidAt,
                last_status_check = invoice.LastStatusCheck,
                transactions = invoice.Transactions.Select(t => new
                {
                    transaction_hash = t.TransactionHash,
                    amount = t.Amount,
                    currency = t.Currency,
                    detected_at = t.DetectedAt,
                    confirmations = t.Confirmations
                })
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting invoice by order ID {OrderId}", orderId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Manually refresh invoice status from CryptoCloud API
    /// </summary>
    /// <param name="invoiceUuid">Invoice UUID to refresh</param>
    /// <returns>Updated invoice status</returns>
    /// <remarks>
    /// Use this endpoint to manually check if a payment has been received.
    /// The background service also automatically checks pending invoices every 60 seconds.
    /// </remarks>
    /// <response code="200">Status refreshed successfully</response>
    /// <response code="404">Invoice not found or refresh failed</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{invoiceUuid}/refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RefreshInvoiceStatus(string invoiceUuid)
    {
        try
        {
            // Normalize UUID
            if (!invoiceUuid.StartsWith("INV-"))
            {
                invoiceUuid = $"INV-{invoiceUuid}";
            }

            var success = await invoiceService.UpdateInvoiceStatusAsync(invoiceUuid);

            if (!success)
            {
                return NotFound(new { message = "Failed to refresh invoice status" });
            }

            var invoice = await invoiceService.GetInvoiceAsync(invoiceUuid);

            return Ok(new
            {
                success = true,
                status = invoice?.Status,
                received_amount = invoice?.ReceivedAmount,
                paid_at = invoice?.PaidAt,
                last_status_check = invoice?.LastStatusCheck
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error refreshing invoice status {InvoiceUuid}", invoiceUuid);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get payment statistics and analytics
    /// </summary>
    /// <returns>Aggregated payment statistics</returns>
    /// <remarks>
    /// Returns comprehensive statistics including:
    /// - Total number of invoices (all statuses)
    /// - Paid invoices count
    /// - Pending invoices count
    /// - Today's and this month's invoice counts
    /// - Total amount received in USD
    /// - Success rate percentage
    /// </remarks>
    /// <response code="200">Statistics retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            InvoiceStatistics? stats = await invoiceService.GetStatisticsAsync();

            return Ok(new
            {
                total_invoices = stats.TotalInvoices,
                paid_invoices = stats.PaidInvoices,
                pending_invoices = stats.PendingInvoices,
                canceled_invoices = stats.CanceledInvoices,
                today_invoices = stats.TodayInvoices,
                this_month_invoices = stats.ThisMonthInvoices,
                total_amount_usd = stats.TotalAmountUsd,
                success_rate = stats.TotalInvoices > 0 
                    ? Math.Round((double)stats.PaidInvoices / stats.TotalInvoices * 100, 2) 
                    : 0
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting statistics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }
}

/// <summary>
/// Request model for creating a payment invoice
/// </summary>
public class CreatePaymentRequest
{
    /// <summary>
    /// Your internal order identifier (required)
    /// </summary>
    /// <example>ORDER-12345</example>
    public string OrderId { get; set; } = string.Empty;
    
    /// <summary>
    /// Payment amount in the specified currency (required)
    /// </summary>
    /// <example>100.00</example>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Fiat currency code (default: USD)
    /// </summary>
    /// <example>USD</example>
    public string? Currency { get; set; }
    
    /// <summary>
    /// Customer email address (optional)
    /// </summary>
    /// <example>customer@example.com</example>
    public string? Email { get; set; }
    
    /// <summary>
    /// Specific cryptocurrency to use (e.g., USDT_TRC20, BTC, ETH)
    /// </summary>
    /// <example>USDT_TRC20</example>
    public string? Cryptocurrency { get; set; }
    
    /// <summary>
    /// List of available cryptocurrencies for customer to choose from
    /// </summary>
    /// <example>["USDT_TRC20", "BTC", "ETH"]</example>
    public List<string>? AvailableCurrencies { get; set; }
    
    /// <summary>
    /// Hours before invoice expires (default: 24)
    /// </summary>
    /// <example>24</example>
    public int? TimeToPayHours { get; set; }
    
    /// <summary>
    /// Additional minutes before invoice expires
    /// </summary>
    /// <example>0</example>
    public int? TimeToPayMinutes { get; set; }
    
    /// <summary>
    /// Additional metadata as JSON string
    /// </summary>
    /// <example>{"userId": "123", "productId": "PROD-001"}</example>
    public string? Metadata { get; set; }
}
