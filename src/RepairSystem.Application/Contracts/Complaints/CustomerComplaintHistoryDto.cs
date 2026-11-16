namespace RepairSystem.Application.Contracts.Complaints;

public sealed class CustomerComplaintHistoryDto
{
    public int HistoryId { get; set; }

    public string? PreviousStatus { get; set; }

    public string NewStatus { get; set; } = string.Empty;

    public string ChangedByName { get; set; } = string.Empty;

    public string ChangedByRole { get; set; } = string.Empty;

    public DateTime ChangedAt { get; set; }
}