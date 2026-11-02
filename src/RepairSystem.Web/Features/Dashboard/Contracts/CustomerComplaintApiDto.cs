using System.Text.Json.Serialization;

namespace RepairSystem.Web.Features.Dashboard.Contracts;

public sealed class CustomerComplaintApiDto
{
    [JsonPropertyName("complaintId")]
    public int ComplaintId { get; init; }

    [JsonPropertyName("ticketNumber")]
    public string TicketNumber { get; init; } = string.Empty;

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; init; } = string.Empty;

    [JsonPropertyName("deviceBrand")]
    public string DeviceBrand { get; init; } = string.Empty;

    [JsonPropertyName("deviceModel")]
    public string DeviceModel { get; init; } = string.Empty;

    [JsonPropertyName("issueDescription")]
    public string IssueDescription { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; init; }
}