
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

    public void AssignToTechnician(int technicianId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(technicianId);

        if (Status != ComplaintStatus.Submitted)
        {
            throw new InvalidOperationException(
                "Only submitted complaints can be assigned.");
        }

        if (AssignedTechnicianId.HasValue)
        {
            throw new InvalidOperationException(
                "This complaint is already assigned to a technician.");
        }

        AssignedTechnicianId = technicianId;

        ChangeStatus(ComplaintStatus.Assigned);
    }

    public void ChangeStatus(ComplaintStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus))
        {
            throw new ArgumentOutOfRangeException(
                nameof(newStatus),
                newStatus,
                "The requested complaint status is invalid.");
        }

        if (newStatus == Status)
        {
            throw new InvalidOperationException(
                "The complaint is already in this status.");
        }

        if (!IsTransitionAllowed(Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Transition from {Status} to {newStatus} is not allowed.");
        }

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    private static bool IsTransitionAllowed(
        ComplaintStatus currentStatus,
        ComplaintStatus newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            (ComplaintStatus.Submitted, ComplaintStatus.Assigned)
                => true,

            (ComplaintStatus.Assigned, ComplaintStatus.InDiagnosis)
                => true,

            (ComplaintStatus.InDiagnosis, ComplaintStatus.QuotePending)
                => true,

            (ComplaintStatus.QuotePending,
                ComplaintStatus.Approved or ComplaintStatus.Cancelled)
                => true,

            (ComplaintStatus.Approved, ComplaintStatus.InRepair)
                => true,

            (ComplaintStatus.InRepair, ComplaintStatus.Completed)
                => true,

            _ => false
        };
    }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
