using RepairSystem.Domain.Enums;

namespace RepairSystem.Domain.Entities;

public class Complaint
{
    public int ComplaintId { get; set; }

    public string TicketNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public User Customer { get; set; } = null!;

    public int? AssignedTechnicianId { get; set; }
    public User? AssignedTechnician { get; set; }

    public string DeviceType { get; set; } = string.Empty;
    public string DeviceBrand { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string IssueDescription { get; set; } = string.Empty;

    public bool ClaimedWarranty { get; set; }

    public ComplaintStatus Status { get; set; } =
        ComplaintStatus.Submitted;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}