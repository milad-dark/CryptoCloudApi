using CryptoCloudApi.Data;
using CryptoCloudApi.Models.DTOs.Requests;
using CryptoCloudApi.Models.DTOs.Responses;
using CryptoCloudApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoCloudApi.Services;

/// <summary>
/// Service for managing payment invoices and integrating with CryptoCloud API
/// </summary>
public class InvoiceManagementService
{
    private readonly PaymentDbContext _dbContext;
    private readonly CryptoCloudApiService _cryptoCloudApi;
    private readonly ILogger<InvoiceManagementService> _logger;

    public InvoiceManagementService(
        PaymentDbContext dbContext,
        CryptoCloudApiService cryptoCloudApi,
        ILogger<InvoiceManagementService> logger)
    {
        _dbContext = dbContext;
        _cryptoCloudApi = cryptoCloudApi;
        _logger = logger;
    }

    /// <summary>
    /// Create a new payment invoice
    /// </summary>
    public async Task<PaymentInvoice?> CreateInvoiceAsync(
        string orderId,
        decimal amount,
        string? currency = null,
        string? email = null,
        AdditionalFields? additionalFields = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if invoice already exists for this order
            PaymentInvoice? existingInvoice = await _dbContext.PaymentInvoices
                .FirstOrDefaultAsync(i => i.OrderId == orderId && i.Status != "canceled", cancellationToken);

            if (existingInvoice != null)
            {
                _logger.LogWarning("Invoice already exists for order {OrderId}: {InvoiceUuid}", 
                    orderId, existingInvoice.InvoiceUuid);
                return existingInvoice;
            }

            // Create invoice via CryptoCloud API
            CreateInvoiceResponse? response = await _cryptoCloudApi.CreateInvoiceAsync(
                amount, orderId, currency, email, additionalFields, cancellationToken);

            if (response?.Status != "success" || response.Result == null)
            {
                _logger.LogError("Failed to create invoice via CryptoCloud API for order {OrderId}", orderId);
                return null;
            }

            InvoiceResult? result = response.Result;

            // Save to database
            PaymentInvoice? invoice = new()
            {
                InvoiceUuid = result.Uuid,
                OrderId = orderId,
                Amount = result.Amount,
                AmountUsd = result.AmountUsd,
                Currency = result.FiatCurrency,
                CryptoCurrency = result.Currency?.FullCode,
                PaymentAddress = result.Address,
                PaymentLink = result.Link,
                Status = result.Status,
                CustomerEmail = email,
                ExpiryDate = ParseDateTime(result.ExpiryDate),
                Fee = result.Fee,
                ServiceFee = result.ServiceFee,
                SideCommission = result.SideCommission,
                TestMode = result.TestMode,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _dbContext.PaymentInvoices.Add(invoice);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Invoice created and saved: {InvoiceUuid} for order {OrderId}", 
                invoice.InvoiceUuid, orderId);

            return invoice;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice for order {OrderId}", orderId);
            return null;
        }
    }

    /// <summary>
    /// Update invoice status from CryptoCloud API
    /// </summary>
    public async Task<bool> UpdateInvoiceStatusAsync(
        string invoiceUuid,
        CancellationToken cancellationToken = default)
    {
        try
        {
            PaymentInvoice? invoice = await _dbContext.PaymentInvoices
                .Include(i => i.Transactions)
                .FirstOrDefaultAsync(i => i.InvoiceUuid == invoiceUuid, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found in database: {InvoiceUuid}", invoiceUuid);
                return false;
            }

            // Get latest info from CryptoCloud
            InvoiceInfoResult? info = await _cryptoCloudApi.GetSingleInvoiceInfoAsync(invoiceUuid, cancellationToken);

            if (info == null)
            {
                _logger.LogWarning("Failed to get invoice info from CryptoCloud API: {InvoiceUuid}", invoiceUuid);
                return false;
            }

            // Update invoice
            string? oldStatus = invoice.Status;
            invoice.Status = info.Status;
            invoice.ReceivedAmount = info.Received;
            invoice.CryptoCurrency = info.Currency?.FullCode;
            invoice.PaymentAddress = info.Address;
            invoice.LastStatusCheck = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            // If payment is completed
            if (info.Status == "paid" || info.Status == "overpaid")
            {
                if (invoice.PaidAt == null)
                {
                    invoice.PaidAt = DateTime.UtcNow;
                }

                // Update or add transactions
                if (info.TxList != null && info.TxList.Any())
                {
                    foreach (string txHash in info.TxList)
                    {
                        if (!string.IsNullOrEmpty(txHash) && 
                            !invoice.Transactions.Any(t => t.TransactionHash == txHash))
                        {
                            invoice.Transactions.Add(new()
                            {
                                TransactionHash = txHash,
                                Amount = info.Received,
                                Currency = info.Currency?.FullCode ?? "UNKNOWN",
                                DetectedAt = DateTime.UtcNow,
                                Confirmations = 0
                            });
                        }
                    }
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            if (oldStatus != invoice.Status)
            {
                _logger.LogInformation("Invoice {InvoiceUuid} status changed: {OldStatus} -> {NewStatus}", 
                    invoiceUuid, oldStatus, invoice.Status);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice status: {InvoiceUuid}", invoiceUuid);
            return false;
        }
    }

    /// <summary>
    /// Process postback notification from CryptoCloud
    /// </summary>
    public async Task<bool> ProcessPostbackAsync(
        PostbackNotification postback,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing postback for invoice {InvoiceId}, status {Status}", 
                postback.InvoiceId, postback.Status);

            PaymentInvoice? invoice = await _dbContext.PaymentInvoices
                .Include(i => i.Transactions)
                .FirstOrDefaultAsync(i => i.InvoiceUuid == postback.InvoiceId, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice not found for postback: {InvoiceId}", postback.InvoiceId);
                return false;
            }

            string? oldStatus = invoice.Status;
            PostbackInvoiceInfo? info = postback.InvoiceInfo;

            if (info != null)
            {
                invoice.Status = info.InvoiceStatus;
                invoice.ReceivedAmount = info.Received;
                invoice.CryptoCurrency = info.Currency?.FullCode;
                invoice.PaymentAddress = info.Address;
                invoice.Fee = info.Fee;
                invoice.ServiceFee = info.ServiceFee;
                invoice.UpdatedAt = DateTime.UtcNow;

                if (info.InvoiceStatus == "paid" || info.InvoiceStatus == "overpaid")
                {
                    if (invoice.PaidAt == null)
                    {
                        invoice.PaidAt = ParseDateTime(info.DateFinished) ?? DateTime.UtcNow;
                    }

                    // Add transactions
                    if (info.TxList != null && info.TxList.Any())
                    {
                        foreach (string txHash in info.TxList)
                        {
                            if (!string.IsNullOrEmpty(txHash) && 
                                !invoice.Transactions.Any(t => t.TransactionHash == txHash))
                            {
                                invoice.Transactions.Add(new()
                                {
                                    TransactionHash = txHash,
                                    Amount = postback.AmountCrypto,
                                    Currency = postback.Currency,
                                    DetectedAt = DateTime.UtcNow,
                                    Confirmations = 0
                                });
                            }
                        }
                    }
                }
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Postback processed successfully for invoice {InvoiceId}. Status: {OldStatus} -> {NewStatus}", 
                postback.InvoiceId, oldStatus, invoice.Status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing postback for invoice {InvoiceId}", postback.InvoiceId);
            return false;
        }
    }

    /// <summary>
    /// Get invoice by UUID
    /// </summary>
    public async Task<PaymentInvoice?> GetInvoiceAsync(
        string invoiceUuid,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentInvoices
            .Include(i => i.Transactions)
            .FirstOrDefaultAsync(i => i.InvoiceUuid == invoiceUuid, cancellationToken);
    }

    /// <summary>
    /// Get invoice by order ID
    /// </summary>
    public async Task<PaymentInvoice?> GetInvoiceByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentInvoices
            .Include(i => i.Transactions)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(i => i.OrderId == orderId, cancellationToken);
    }

    /// <summary>
    /// Get all pending invoices that need status check
    /// </summary>
    public async Task<List<PaymentInvoice>> GetPendingInvoicesAsync(
        int maxAgeHours = 24,
        CancellationToken cancellationToken = default)
    {
        DateTime cutoffDate = DateTime.UtcNow.AddHours(-maxAgeHours);

        return await _dbContext.PaymentInvoices
            .Where(i => 
                (i.Status == "created" || i.Status == "partial") &&
                i.CreatedAt >= cutoffDate &&
                (i.ExpiryDate == null || i.ExpiryDate > DateTime.UtcNow))
            .OrderBy(i => i.LastStatusCheck ?? DateTime.MinValue)
            .Take(100) // Limit to 100 per batch
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get invoice statistics
    /// </summary>
    public async Task<InvoiceStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        DateTime today = DateTime.UtcNow.Date;
        DateTime thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return new InvoiceStatistics
        {
            TotalInvoices = await _dbContext.PaymentInvoices.CountAsync(cancellationToken),
            PaidInvoices = await _dbContext.PaymentInvoices.CountAsync(i => i.Status == "paid" || i.Status == "overpaid", cancellationToken),
            PendingInvoices = await _dbContext.PaymentInvoices.CountAsync(i => i.Status == "created" || i.Status == "partial", cancellationToken),
            CanceledInvoices = await _dbContext.PaymentInvoices.CountAsync(i => i.Status == "canceled", cancellationToken),
            TodayInvoices = await _dbContext.PaymentInvoices.CountAsync(i => i.CreatedAt >= today, cancellationToken),
            ThisMonthInvoices = await _dbContext.PaymentInvoices.CountAsync(i => i.CreatedAt >= thisMonth, cancellationToken),
            TotalAmountUsd = await _dbContext.PaymentInvoices.Where(i => i.Status == "paid" || i.Status == "overpaid").SumAsync(i => i.AmountUsd, cancellationToken)
        };
    }

    private DateTime? ParseDateTime(string? dateTimeString)
    {
        if (string.IsNullOrEmpty(dateTimeString))
            return null;

        if (DateTime.TryParse(dateTimeString, out var result))
            return DateTime.SpecifyKind(result, DateTimeKind.Utc);

        return null;
    }
}

/// <summary>
/// Invoice statistics model
/// </summary>
public class InvoiceStatistics
{
    public int TotalInvoices { get; set; }
    public int PaidInvoices { get; set; }
    public int PendingInvoices { get; set; }
    public int CanceledInvoices { get; set; }
    public int TodayInvoices { get; set; }
    public int ThisMonthInvoices { get; set; }
    public decimal TotalAmountUsd { get; set; }
}
