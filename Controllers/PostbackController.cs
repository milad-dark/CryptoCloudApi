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
/// Controller for receiving postback notifications from CryptoCloud
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PostbackController(
    InvoiceManagementService invoiceService,
    IOptions<CryptoCloudSettings> settings,
    ILogger<PostbackController> logger) : ControllerBase
{
    /// <summary>
    /// Receive postback notification from CryptoCloud when payment is completed
    /// </summary>
    /// <remarks>
    /// This endpoint is called automatically by CryptoCloud when a payment is completed.
    /// Configure this URL in your CryptoCloud project settings as the postback URL.
    /// Example: https://yourdomain.com/api/postback/notify
    /// </remarks>
    [HttpPost("notify")]
    public async Task<IActionResult> ReceiveNotification([FromBody] PostbackNotification notification)
    {
        try
        {
            if (notification == null)
            {
                logger.LogWarning("Received null postback notification");
                return BadRequest(new { message = "Invalid notification data" });
            }

            logger.LogInformation("Received postback for invoice {InvoiceId}, status {Status}", 
                notification.InvoiceId, notification.Status);

            // Verify JWT token signature
            if (!VerifyToken(notification.Token, notification.InvoiceId))
            {
                logger.LogWarning("Invalid JWT token for invoice {InvoiceId}", notification.InvoiceId);
                return Unauthorized(new { message = "Invalid token signature" });
            }

            // Process the postback
            var success = await invoiceService.ProcessPostbackAsync(notification);

            if (!success)
            {
                logger.LogError("Failed to process postback for invoice {InvoiceId}", notification.InvoiceId);
                return StatusCode(500, new { message = "Failed to process notification" });
            }

            logger.LogInformation("Postback processed successfully for invoice {InvoiceId}", notification.InvoiceId);

            // Return success response
            return Ok(new { message = "Postback received", invoice_id = notification.InvoiceId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing postback notification");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    /// <summary>
    /// Test endpoint to verify postback URL is accessible
    /// </summary>
    [HttpGet("test")]
    public IActionResult TestEndpoint()
    {
        return Ok(new 
        { 
            message = "Postback endpoint is working",
            timestamp = DateTime.UtcNow,
            endpoint = $"{Request.Scheme}://{Request.Host}/api/postback/notify"
        });
    }

    /// <summary>
    /// Verify JWT token from CryptoCloud postback
    /// </summary>
    private bool VerifyToken(string token, string invoiceId)
    {
        try
        {
            if (string.IsNullOrEmpty(settings.Value.SecretKey))
            {
                logger.LogWarning("SecretKey not configured, skipping token verification");
                return true; // Allow in development if secret not set
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(settings.Value.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minute clock skew
            };

            tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);

            var jwtToken = (JwtSecurityToken)validatedToken;

            // Verify invoice UUID is in the token
            var uuidClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "id" || c.Type == "uuid")?.Value;
            
            if (string.IsNullOrEmpty(uuidClaim))
            {
                logger.LogWarning("JWT token does not contain invoice UUID");
                return false;
            }

            // The UUID in token might be without INV- prefix
            var normalizedTokenUuid = uuidClaim.StartsWith("INV-") ? uuidClaim : $"INV-{uuidClaim}";
            var normalizedInvoiceId = invoiceId.StartsWith("INV-") ? invoiceId : $"INV-{invoiceId}";

            if (normalizedTokenUuid != normalizedInvoiceId)
            {
                logger.LogWarning("Invoice UUID mismatch. Token: {TokenUuid}, Invoice: {InvoiceId}", 
                    normalizedTokenUuid, normalizedInvoiceId);
                return false;
            }

            return true;
        }
        catch (SecurityTokenExpiredException)
        {
            logger.LogWarning("JWT token has expired for invoice {InvoiceId}", invoiceId);
            return false;
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning(ex, "Invalid JWT token for invoice {InvoiceId}", invoiceId);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying JWT token for invoice {InvoiceId}", invoiceId);
            return false;
        }
    }
}
