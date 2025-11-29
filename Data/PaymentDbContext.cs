using CryptoCloudApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CryptoCloudApi.Data;

/// <summary>
/// Database context for CryptoCloud payment system
/// </summary>
public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentInvoice> PaymentInvoices { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PaymentInvoice
        modelBuilder.Entity<PaymentInvoice>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.InvoiceUuid)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.InvoiceUuid)
                .IsUnique();

            entity.Property(e => e.OrderId)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(e => e.OrderId);

            entity.Property(e => e.Amount)
                .HasPrecision(18, 8);

            entity.Property(e => e.AmountUsd)
                .HasPrecision(18, 8);

            entity.Property(e => e.ReceivedAmount)
                .HasPrecision(18, 8);

            entity.Property(e => e.Fee)
                .HasPrecision(18, 8);

            entity.Property(e => e.ServiceFee)
                .HasPrecision(18, 8);

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(10);

            entity.Property(e => e.CryptoCurrency)
                .HasMaxLength(50);

            entity.Property(e => e.PaymentAddress)
                .HasMaxLength(200);

            entity.Property(e => e.PaymentLink)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(e => e.Status);

            entity.Property(e => e.CustomerEmail)
                .HasMaxLength(255);

            entity.Property(e => e.SideCommission)
                .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.HasIndex(e => e.CreatedAt);

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            // Configure relationship
            entity.HasMany(e => e.Transactions)
                .WithOne(t => t.PaymentInvoice)
                .HasForeignKey(t => t.PaymentInvoiceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PaymentTransaction
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.TransactionHash)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(e => e.TransactionHash);

            entity.Property(e => e.Amount)
                .HasPrecision(18, 8);

            entity.Property(e => e.Currency)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.DetectedAt)
                .IsRequired();
        });
    }
}
