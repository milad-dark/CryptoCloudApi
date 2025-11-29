using CryptoCloudApi.Models.Configuration;
using CryptoCloudApi.Models.DTOs.Responses;
using CryptoCloudApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

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
    /// <param name="notification">Postback notification containing payment details and JWT token</param>
    /// <returns>Confirmation of postback receipt</returns>
    /// <remarks>
    /// This endpoint is called automatically by CryptoCloud when a payment is completed.
    /// 
    /// **Configuration:**
    /// Configure this URL in your CryptoCloud project settings as the postback URL:
    /// ```
    /// https://yourdomain.com/api/postback/notify
    /// ```
    /// 
    /// **Security:**
    /// All postback notifications are verified using JWT token signatures.
    /// The token is signed with your secret key using HS256 algorithm.
    /// Invalid or expired tokens are rejected.
    /// 
    /// **Sample Postback:**
    /// ```json
    /// {
    ///   "status": "success",
    ///   "invoice_id": "INV-XXXXXXXX",
    ///   "amount_crypto": 50.123456,
    ///   "currency": "USDT_TRC20",
    ///   "order_id": "ORDER-001",
    ///   "token": "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9..."
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">Postback received and processed successfully</response>
    /// <response code="400">Invalid notification data</response>
    /// <response code="401">Invalid or expired JWT token</response>
    /// <response code="500">Failed to process notification</response>
    [HttpPost("notify")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReceiveNotification([FromBody] PostbackNotification notification)
    {
        try
        {
            if (notification == null)
            {
                _logger.LogWarning("Received null postback notification");
                return BadRequest(new { message = "Invalid notification data" });
            }

            _logger.LogInformation("Received postback for invoice {InvoiceId}, status {Status}", 
                notification.InvoiceId, notification.Status);

            // Verify JWT token signature
            if (!VerifyToken(notification.Token, notification.InvoiceId))
            {
                _logger.LogWarning("Invalid JWT token for invoice {InvoiceId}", notification.InvoiceId);
                return Unauthorized(new { message = "Invalid token signature" });
            }

            // Process the postback
            var success = await _invoiceService.ProcessPostbackAsync(notification);

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
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Test endpoint to verify postback URL is accessible
    /// </summary>
    /// <returns>Status information and endpoint URL</returns>
    /// <remarks>
    /// Use this endpoint to verify your postback webhook is properly configured and accessible.
    /// 
    /// **Usage:**
    /// ```bash
    /// curl https://yourdomain.com/api/postback/test
    /// ```
    /// 
    /// **For local development with ngrok:**
    /// 1. Start ngrok: `ngrok http 5001`
    /// 2. Test the endpoint with ngrok URL
    /// 3. Configure the ngrok URL in CryptoCloud dashboard
    /// </remarks>
    /// <response code="200">Postback endpoint is working</response>
    [HttpGet("test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult TestEndpoint()
    {
        return Ok(new 
        { 
            message = "Postback endpoint is working",
            timestamp = DateTime.UtcNow,
            endpoint = $"{Request.Scheme}://{Request.Host}/api/postback/notify",
            note = "Configure this URL as your postback URL in CryptoCloud dashboard"
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
