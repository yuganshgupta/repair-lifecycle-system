
namespace RepairSystem.Application.Contracts.RepairWorkflow;

public sealed record QuoteLineRequest(
    int ItemType,
    string Description,
    int Quantity,
    decimal UnitPrice);

public sealed record SubmitQuoteRequest(
    string DiagnosticFindings,
    bool IsWarrantyValid,
    string? WarrantyRejectionReason,
    List<QuoteLineRequest> Items);

public sealed record QuoteDecisionRequest(
    bool? Accept,
    string? Reason);

public sealed record QuoteLineDto(
    string ItemType,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice);

public sealed record QuoteDto(
    int QuoteId,
    string DiagnosticFindings,
    bool IsWarrantyValid,
    string? WarrantyRejectionReason,
    decimal InspectionFee,
    decimal InspectionFeeDue,
    decimal EstimatedTotalCost,
    string CustomerDecision,
    string? DecisionReason,
    DateTime? DecisionTimestamp,
    IReadOnlyList<QuoteLineDto> Items);

public sealed record EvidenceDto(
    int AttachmentId,
    string Purpose,
    long FileSizeBytes,
    DateTime UploadedAt);

public sealed record RepairWorkflowDto(
    int ComplaintId,
    string TicketNumber,
    string Status,
    decimal ConfiguredInspectionFee,
    QuoteDto? Quote,
    IReadOnlyList<EvidenceDto> Attachments);

public enum WorkflowError
{
    Invalid,
    Forbidden,
    NotFound,
    Conflict
}

public sealed class WorkflowException : Exception
{
    public WorkflowError Error { get; }

    public WorkflowException(
        WorkflowError error,
        string message) : base(message)
    {
        Error = error;
    }
}
