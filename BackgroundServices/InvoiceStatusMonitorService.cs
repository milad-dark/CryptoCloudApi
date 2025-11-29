using CryptoCloudApi.Models.Configuration;
using CryptoCloudApi.Services;
using Microsoft.Extensions.Options;

namespace CryptoCloudApi.BackgroundServices;

/// <summary>
/// Background service that periodically checks the status of pending payment invoices
/// </summary>
public class InvoiceStatusMonitorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CryptoCloudSettings _settings;
    private readonly ILogger<InvoiceStatusMonitorService> _logger;

    public InvoiceStatusMonitorService(
        IServiceProvider serviceProvider,
        IOptions<CryptoCloudSettings> settings,
        ILogger<InvoiceStatusMonitorService> logger)
    {
        _serviceProvider = serviceProvider;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Invoice Status Monitor Service started");

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
                _logger.LogError(ex, "Error in invoice status monitoring cycle");
            }

            // Wait for the configured interval before next check
            var delay = TimeSpan.FromSeconds(_settings.StatusCheckIntervalSeconds);
            _logger.LogDebug("Waiting {Seconds} seconds until next status check", delay.TotalSeconds);
            
            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("Invoice Status Monitor Service stopped");
    }

    private async Task CheckPendingInvoicesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var invoiceService = scope.ServiceProvider.GetRequiredService<InvoiceManagementService>();

        _logger.LogInformation("Starting invoice status check cycle");

        // Get all pending invoices
        var pendingInvoices = await invoiceService.GetPendingInvoicesAsync(
            _settings.MonitoringPeriodHours, 
            cancellationToken);

        if (pendingInvoices.Count == 0)
        {
            _logger.LogInformation("No pending invoices to check");
            return;
        }

        _logger.LogInformation("Found {Count} pending invoice(s) to check", pendingInvoices.Count);

        var updatedCount = 0;
        var errorCount = 0;

        // Check each invoice
        foreach (var invoice in pendingInvoices)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                _logger.LogDebug("Checking status for invoice {InvoiceUuid}", invoice.InvoiceUuid);

                var success = await invoiceService.UpdateInvoiceStatusAsync(
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
                _logger.LogError(ex, "Error checking status for invoice {InvoiceUuid}", invoice.InvoiceUuid);
                errorCount++;
            }
        }

        _logger.LogInformation(
            "Invoice status check cycle completed. Checked: {Total}, Updated: {Updated}, Errors: {Errors}",
            pendingInvoices.Count, updatedCount, errorCount);
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Invoice Status Monitor Service starting with interval: {Interval} seconds, monitoring period: {Period} hours",
            _settings.StatusCheckIntervalSeconds,
            _settings.MonitoringPeriodHours);

        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invoice Status Monitor Service stopping");
        await base.StopAsync(cancellationToken);
    }
}
