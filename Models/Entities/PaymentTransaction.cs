namespace CryptoCloudApi.Models.Entities;

/// <summary>
/// Database entity for individual cryptocurrency transactions
/// </summary>
public class PaymentTransaction
{
    public long Id { get; set; }

    /// <summary>
    /// Foreign key to PaymentInvoice
    /// </summary>
    public long PaymentInvoiceId { get; set; }

    /// <summary>
    /// Transaction hash from blockchain
    /// </summary>
    public string TransactionHash { get; set; } = string.Empty;

    /// <summary>
    /// Amount received in this transaction
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Cryptocurrency code
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// When transaction was detected
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of confirmations
    /// </summary>
    public int Confirmations { get; set; }

    /// <summary>
    /// Navigation property
    /// </summary>
    public virtual PaymentInvoice PaymentInvoice { get; set; } = null!;
}
