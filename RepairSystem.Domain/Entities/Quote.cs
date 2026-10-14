
namespace RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

public class Quote
{
    public int QuoteId { get; set; }

    public int ComplaintId { get; set; }

    public Complaint Complaint { get; set; } = null!;

    public int TechnicianId { get; set;  }

    public User Technician { get; set; } = null!;

    public bool IsWarrantyValid { get; set; }

    public string? WarrantyRejectionReason { get; set; }

    public decimal InspectionFee { get; set; } = 0m;

    public decimal EstimatedTotalCost { get; set; } 

    public QuoteDecision CustomerDecision { get; set; } = QuoteDecision.Pending;

    public DateTime? DecisionTimestamp { get; set; } 

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}
