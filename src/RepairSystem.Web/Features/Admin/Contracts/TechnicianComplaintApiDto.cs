using System.Text.Json.Serialization;

namespace RepairSystem.Web.Features.Dashboard.Contracts;

public sealed class TechnicianComplaintApiDto
{
    [JsonPropertyName("complaintId")]
    public int ComplaintId { get; set; }

    [JsonPropertyName("ticketNumber")]
    public string TicketNumber { get; set; } = string.Empty;

    [JsonPropertyName("customerName")]
    public string CustomerName { get; set; } = string.Empty;

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; } = string.Empty;

    [JsonPropertyName("deviceBrand")]
    public string DeviceBrand { get; set; } = string.Empty;

    [JsonPropertyName("deviceModel")]
    public string DeviceModel { get; set; } = string.Empty;

    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; set; }

    [JsonPropertyName("issueDescription")]
    public string IssueDescription { get; set; } = string.Empty;

    [JsonPropertyName("claimedWarranty")]
    public bool ClaimedWarranty { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}