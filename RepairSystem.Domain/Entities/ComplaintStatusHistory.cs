using RepairSystem.Domain.Enums;

namespace RepairSystem.Domain.Entities;

public class ComplaintStatusHistory
{
    public int ComplaintStatusHistoryId { get; set; }

    public int ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;

    public ComplaintStatus? PreviousStatus { get; set; }

    public ComplaintStatus NewStatus { get; set; }

    public int ChangedByUserId { get; set; }
    public User ChangedByUser { get; set; } = null!;

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}