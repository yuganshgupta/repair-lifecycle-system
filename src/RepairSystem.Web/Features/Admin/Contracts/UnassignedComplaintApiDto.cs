using System.Text.Json.Serialization;

namespace RepairSystem.Web.Features.Admin.Contracts;

public sealed class UnassignedComplaintApiDto
{
    public int ComplaintId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceBrand { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string IssueDescription { get; set; } = string.Empty;
    public bool ClaimedWarranty { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    public int SelectedTechnicianId { get; set; }
}