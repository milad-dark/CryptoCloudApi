using CryptoCloudApi.Models.Configuration;
using CryptoCloudApi.Services;
using Microsoft.Extensions.Options;

namespace CryptoCloudApi.BackgroundServices;

/// <summary>
/// Background service that periodically checks the status of pending payment invoices
/// </summary>
public class InvoiceStatusMonitorService(
    IServiceProvider serviceProvider,
    IOptions<CryptoCloudSettings> settings,
    ILogger<InvoiceStatusMonitorService> logger) : BackgroundService
{
    private readonly CryptoCloudSettings _settings = settings.Value;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Invoice Status Monitor Service started");

        // Wait a bit before starting to allow app to fully initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckPendingInvoicesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in invoice status monitoring cycle");
            }

            // Wait for the configured interval before next check
            TimeSpan delay = TimeSpan.FromSeconds(_settings.StatusCheckIntervalSeconds);
            logger.LogDebug("Waiting {Seconds} seconds until next status check", delay.TotalSeconds);
            
            await Task.Delay(delay, stoppingToken);
        }

        logger.LogInformation("Invoice Status Monitor Service stopped");
    }

    private async Task CheckPendingInvoicesAsync(CancellationToken cancellationToken)
    {
        using IServiceScope? scope = serviceProvider.CreateScope();
        InvoiceManagementService? invoiceService = scope.ServiceProvider.GetRequiredService<InvoiceManagementService>();

        logger.LogInformation("Starting invoice status check cycle");

        // Get all pending invoices
        List<Models.Entities.PaymentInvoice>? pendingInvoices = await invoiceService.GetPendingInvoicesAsync(
            _settings.MonitoringPeriodHours, 
            cancellationToken);

        if (pendingInvoices.Count == 0)
        {
            logger.LogInformation("No pending invoices to check");
            return;
        }

        logger.LogInformation("Found {Count} pending invoice(s) to check", pendingInvoices.Count);

        int updatedCount = 0;
        int errorCount = 0;

        // Check each invoice
        foreach (Models.Entities.PaymentInvoice invoice in pendingInvoices)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                logger.LogDebug("Checking status for invoice {InvoiceUuid}", invoice.InvoiceUuid);

                bool success = await invoiceService.UpdateInvoiceStatusAsync(
                    invoice.InvoiceUuid, 
                    cancellationToken);

                if (success)
                {
                    updatedCount++;
                }
                else
                {
                    errorCount++;
                }

                // Small delay between requests to avoid rate limiting
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking status for invoice {InvoiceUuid}", invoice.InvoiceUuid);
                errorCount++;
            }
        }

        logger.LogInformation(
            "Invoice status check cycle completed. Checked: {Total}, Updated: {Updated}, Errors: {Errors}",
            pendingInvoices.Count, updatedCount, errorCount);
    }

    public async override Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Invoice Status Monitor Service starting with interval: {Interval} seconds, monitoring period: {Period} hours",
            _settings.StatusCheckIntervalSeconds,
            _settings.MonitoringPeriodHours);

        await base.StartAsync(cancellationToken);
    }

    public async override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Invoice Status Monitor Service stopping");
        await base.StopAsync(cancellationToken);
    }
}
