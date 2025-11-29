using System.Text.Json.Serialization;

namespace CryptoCloudApi.Models.DTOs.Requests;

/// <summary>
/// Request model for getting invoice information
/// </summary>
public class InvoiceInfoRequest
{
    [JsonPropertyName("uuids")]
    public List<string> Uuids { get; set; } = new();
}
