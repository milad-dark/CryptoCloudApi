using CryptoCloudApi.Models.Configuration;
using CryptoCloudApi.Models.DTOs.Requests;
using CryptoCloudApi.Models.DTOs.Responses;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CryptoCloudApi.Services;

/// <summary>
/// Service for interacting with CryptoCloud API
/// </summary>
public class CryptoCloudApiService
{
    private readonly HttpClient _httpClient;
    private readonly CryptoCloudSettings _settings;
    private readonly ILogger<CryptoCloudApiService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public CryptoCloudApiService(
        HttpClient httpClient,
        IOptions<CryptoCloudSettings> settings,
        ILogger<CryptoCloudApiService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        // Configure HttpClient
        _httpClient.BaseAddress = new Uri(_settings.ApiBaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_settings.ApiKey}");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Create a new payment invoice
    /// </summary>
    public async Task<CreateInvoiceResponse?> CreateInvoiceAsync(
        decimal amount,
        string orderId,
        string? currency = null,
        string? email = null,
        AdditionalFields? additionalFields = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            CreateInvoiceRequest? request = new()
            {
                ShopId = _settings.ShopId,
                Amount = amount,
                Currency = currency ?? _settings.DefaultCurrency,
                OrderId = orderId,
                //Email = email,
                //AddFields = additionalFields
            };

            string? json = JsonSerializer.Serialize(request, _jsonOptions);
            StringContent? content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Creating invoice for order {OrderId}, amount {Amount} {Currency}", 
                orderId, amount, request.Currency);

            HttpResponseMessage? response = await _httpClient.PostAsync("/v2/invoice/create", content, cancellationToken);
            string? responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create invoice. Status: {Status}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return null;
            }

            CreateInvoiceResponse? result = JsonSerializer.Deserialize<CreateInvoiceResponse>(responseContent, _jsonOptions);
            
            if (result?.Status == "success")
            {
                _logger.LogInformation("Invoice created successfully: {Uuid}", result.Result?.Uuid);
            }
            else
            {
                _logger.LogWarning("Invoice creation returned non-success status: {Status}", result?.Status);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice for order {OrderId}", orderId);
            return null;
        }
    }

    /// <summary>
    /// Get information about one or more invoices
    /// </summary>
    public async Task<InvoiceInfoResponse?> GetInvoiceInfoAsync(
        List<string> invoiceUuids,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (invoiceUuids.Count == 0)
            {
                _logger.LogWarning("GetInvoiceInfoAsync called with empty UUID list");
                return null;
            }

            if (invoiceUuids.Count > 100)
            {
                _logger.LogWarning("GetInvoiceInfoAsync called with more than 100 UUIDs, trimming to 100");
                invoiceUuids = invoiceUuids.Take(100).ToList();
            }

            InvoiceInfoRequest? request = new() { Uuids = invoiceUuids };
            string? json = JsonSerializer.Serialize(request, _jsonOptions);
            StringContent? content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Fetching info for {Count} invoice(s)", invoiceUuids.Count);

            HttpResponseMessage? response = await _httpClient.PostAsync("/v2/invoice/merchant/info", content, cancellationToken);
            string? responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get invoice info. Status: {Status}, Response: {Response}", 
                    response.StatusCode, responseContent);
                return null;
            }

            InvoiceInfoResponse? result = JsonSerializer.Deserialize<InvoiceInfoResponse>(responseContent, _jsonOptions);

            if (result?.Status == "success")
            {
                _logger.LogInformation("Retrieved info for {Count} invoice(s)", result.Result?.Count ?? 0);
            }
            else
            {
                _logger.LogWarning("Invoice info query returned non-success status: {Status}", result?.Status);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice info");
            return null;
        }
    }

    /// <summary>
    /// Get information about a single invoice
    /// </summary>
    public async Task<InvoiceInfoResult?> GetSingleInvoiceInfoAsync(
        string invoiceUuid,
        CancellationToken cancellationToken = default)
    {
        var response = await GetInvoiceInfoAsync(new List<string> { invoiceUuid }, cancellationToken);
        return response?.Result?.FirstOrDefault();
    }

    /// <summary>
    /// Check if API connection is working
    /// </summary>
    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Testing CryptoCloud API connection");
            
            // Try to get info for a non-existent invoice - we just want to verify auth works
            InvoiceInfoRequest? request = new() { Uuids = new List<string> { "INV-TEST" } };
            string? json = JsonSerializer.Serialize(request, _jsonOptions);
            StringContent? content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage? response = await _httpClient.PostAsync("/v2/invoice/merchant/info", content, cancellationToken);
            
            // If we get 200 or 400 (but not 401/403), it means auth is working
            if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                _logger.LogInformation("CryptoCloud API connection test successful");
                return true;
            }

            _logger.LogWarning("CryptoCloud API connection test failed with status {Status}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing CryptoCloud API connection");
            return false;
        }
    }
}
