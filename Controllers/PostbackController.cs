using CryptoCloudApi.Models.Configuration;
using CryptoCloudApi.Models.DTOs.Responses;
using CryptoCloudApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;

namespace CryptoCloudApi.Controllers;

/// <summary>
/// Webhook endpoint for receiving payment notifications from CryptoCloud
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Webhook / Postback")]
public class PostbackController : ControllerBase
{
    private readonly InvoiceManagementService _invoiceService;
    private readonly CryptoCloudSettings _settings;
    private readonly ILogger<PostbackController> _logger;

    public PostbackController(
        InvoiceManagementService invoiceService,
        IOptions<CryptoCloudSettings> settings,
        ILogger<PostbackController> logger)
    {
        _invoiceService = invoiceService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Receive automatic payment notification from CryptoCloud
    /// </summary>
    /// <returns>Confirmation of postback receipt</returns>
    /// <remarks>
    /// This endpoint is called automatically by CryptoCloud when a payment is completed.
    /// Supports both application/json and application/x-www-form-urlencoded content types.
    /// </remarks>
    [HttpPost("notify")]
    [Consumes("application/json", "application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReceiveNotification()
    {
        PostbackNotification? notification = null;

        try
        {
            // Check content type and parse accordingly
            string? contentType = Request.ContentType?.ToLower() ?? "";

            if (contentType.Contains("application/x-www-form-urlencoded") || Request.HasFormContentType)
            {
                // Parse form-urlencoded data
                IFormCollection? form = await Request.ReadFormAsync();
                notification = new PostbackNotification();

                if (form.TryGetValue("status", out var status))
                    notification.Status = status.ToString();

                if (form.TryGetValue("invoice_id", out var invoiceId))
                    notification.InvoiceId = invoiceId.ToString();

                if (form.TryGetValue("invoice_info", out var invoiceInfo))
                {
                    try
                    {
                        string invoiceInfoJson = invoiceInfo.ToString();
                        if (!string.IsNullOrEmpty(invoiceInfoJson))
                        {
                            // Validate it looks like JSON before attempting to deserialize
                            invoiceInfoJson = invoiceInfoJson.Trim();
                            if (invoiceInfoJson.StartsWith("{") || invoiceInfoJson.StartsWith("["))
                            {
                                notification.InvoiceInfo = JsonSerializer.Deserialize<PostbackInvoiceInfo>(invoiceInfoJson);
                            }
                            else
                            {
                                _logger.LogWarning("invoice_info field does not contain valid JSON format. Value: {InvoiceInfo}",
                                    invoiceInfoJson.Length > 100 ? invoiceInfoJson.Substring(0, 100) + "..." : invoiceInfoJson);
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse invoice_info JSON");
                    }
                }

                if (form.TryGetValue("amount_crypto", out var amountCrypto) 
                    && decimal.TryParse(amountCrypto.ToString(), out var amt))
                    notification.AmountCrypto = amt;

                if (form.TryGetValue("currency", out var currency))
                    notification.Currency = currency.ToString();

                if (form.TryGetValue("order_id", out var orderId))
                    notification.OrderId = orderId.ToString();

                if (form.TryGetValue("token", out var token))
                    notification.Token = token.ToString();

                _logger.LogInformation("Parsed form-urlencoded postback: invoice_id={InvoiceId}, status={Status}, currency={Currency}, amount={Amount}", 
                    notification.InvoiceId, notification.Status, notification.Currency, notification.AmountCrypto);
            }
            else if (contentType.Contains("application/json"))
            {
                // Parse JSON data
                using StreamReader? reader = new StreamReader(Request.Body);
                string? body = await reader.ReadToEndAsync();
                
                if (!string.IsNullOrEmpty(body))
                {
                    notification = JsonSerializer.Deserialize<PostbackNotification>(body);
                    _logger.LogInformation("Parsed JSON postback: invoice_id={InvoiceId}, status={Status}", 
                        notification?.InvoiceId, notification?.Status);
                }
            }
            else
            {
                _logger.LogWarning("Unsupported content type: {ContentType}", Request.ContentType);
                return BadRequest(new { message = $"Unsupported content type: {Request.ContentType}" });
            }

            if (notification == null)
            {
                _logger.LogWarning("Failed to parse postback notification");
                return BadRequest(new { message = "Invalid notification data" });
            }

            _logger.LogInformation("Received postback for invoice {InvoiceId}, status {Status}", 
                notification.InvoiceId, notification.Status);

            // Validate minimal required fields
            if (string.IsNullOrEmpty(notification.Token) || string.IsNullOrEmpty(notification.InvoiceId))
            {
                _logger.LogWarning("Postback missing token or invoice id");
                return BadRequest(new { message = "Missing token or invoice_id" });
            }

            // Verify JWT token signature
            if (!VerifyToken(notification.Token, notification.InvoiceId))
            {
                _logger.LogWarning("Invalid JWT token for invoice {InvoiceId}", notification.InvoiceId);
                return Unauthorized(new { message = "Invalid token signature" });
            }

            // Process the postback
            bool success = await _invoiceService.ProcessPostbackAsync(notification);

            if (!success)
            {
                _logger.LogError("Failed to process postback for invoice {InvoiceId}", notification.InvoiceId);
                return StatusCode(500, new { message = "Failed to process notification" });
            }

            _logger.LogInformation("Postback processed successfully for invoice {InvoiceId}", notification.InvoiceId);

            // Return success response
            return Ok(new { message = "Postback received", invoice_id = notification.InvoiceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing postback notification");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Test endpoint to verify postback URL is accessible
    /// </summary>
    [HttpGet("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult TestEndpoint()
    {
        return Ok(new 
        { 
            message = "Postback endpoint is working",
            timestamp = DateTime.UtcNow,
            endpoint = $"{Request.Scheme}://{Request.Host}/api/postback/notify",
            note = "Configure this URL as your postback URL in CryptoCloud dashboard",
            supported_content_types = new[] { "application/json", "application/x-www-form-urlencoded" }
        });
    }

    /// <summary>
    /// Verify JWT token from CryptoCloud postback
    /// </summary>
    private bool VerifyToken(string token, string invoiceId)
    {
        try
        {
            if (string.IsNullOrEmpty(_settings.SecretKey))
            {
                _logger.LogWarning("SecretKey not configured, skipping token verification");
                return true; // Allow in development if secret not set
            }

            JwtSecurityTokenHandler? tokenHandler = new JwtSecurityTokenHandler();
            byte[]? key = Encoding.UTF8.GetBytes(_settings.SecretKey);

            TokenValidationParameters? validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minute clock skew
            };

            tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            JwtSecurityToken? jwtToken = (JwtSecurityToken)validatedToken;

            // Verify invoice UUID is in the token
            string? uuidClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "id" || c.Type == "uuid")?.Value;
            
            if (string.IsNullOrEmpty(uuidClaim))
            {
                _logger.LogWarning("JWT token does not contain invoice UUID");
                return false;
            }

            // The UUID in token might be without INV- prefix
            string? normalizedTokenUuid = uuidClaim.StartsWith("INV-") ? uuidClaim : $"INV-{uuidClaim}";
            string? normalizedInvoiceId = invoiceId.StartsWith("INV-") ? invoiceId : $"INV-{invoiceId}";

            if (normalizedTokenUuid != normalizedInvoiceId)
            {
                _logger.LogWarning("Invoice UUID mismatch. Token: {TokenUuid}, Invoice: {InvoiceId}", 
                    normalizedTokenUuid, normalizedInvoiceId);
                return false;
            }

            return true;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogWarning("JWT token has expired for invoice {InvoiceId}", invoiceId);
            return false;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid JWT token for invoice {InvoiceId}", invoiceId);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying JWT token for invoice {InvoiceId}", invoiceId);
            return false;
        }
    }
}
