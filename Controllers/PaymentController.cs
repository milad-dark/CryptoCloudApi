using CryptoCloudApi.Models.DTOs.Requests;
using CryptoCloudApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CryptoCloudApi.Controllers;

/// <summary>
/// Controller for managing payment invoices
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentController(
    InvoiceManagementService invoiceService,
    ILogger<PaymentController> logger) : ControllerBase
{

    /// <summary>
    /// Create a new payment invoice
    /// </summary>
    /// <param name="request">Invoice creation request</param>
    [HttpPost("create")]
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
    /// <param name="invoiceUuid">Invoice UUID (with or without INV- prefix)</param>
    [HttpGet("{invoiceUuid}")]
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
    /// Get invoice by order ID
    /// </summary>
    /// <param name="orderId">Your internal order ID</param>
    [HttpGet("order/{orderId}")]
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
    /// <param name="invoiceUuid">Invoice UUID</param>
    [HttpPost("{invoiceUuid}/refresh")]
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
    /// Get payment statistics
    /// </summary>
    [HttpGet("statistics")]
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
