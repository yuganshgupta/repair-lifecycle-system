
using RepairSystem.Domain.Enums;

namespace RepairSystem.Domain.Entities;

public class Quote
{
    private readonly List<QuoteItem> _items = new();

    private Quote()
    {
    }

    public int QuoteId { get; private set; }

    public int ComplaintId { get; private set; }

    public Complaint Complaint { get; private set; } = null!;

    public int TechnicianId { get; private set; }

    public User Technician { get; private set; } = null!;

    public string DiagnosticFindings { get; private set; }
        = string.Empty;

    public bool IsWarrantyValid { get; private set; }

    public string? WarrantyRejectionReason { get; private set; }

    public decimal InspectionFee { get; private set; }

    public decimal EstimatedTotalCost { get; private set; }

    public QuoteDecision CustomerDecision { get; private set; }
        = QuoteDecision.Pending;

    public string? DecisionReason { get; private set; }

    public DateTime? DecisionTimestamp { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<QuoteItem> Items =>
        _items.AsReadOnly();

    public decimal InspectionFeeDue =>
        !IsWarrantyValid &&
        CustomerDecision == QuoteDecision.Rejected
            ? InspectionFee
            : 0m;

    public static Quote Create(
        int complaintId,
        int technicianId,
        bool isWarrantyValid,
        string? warrantyRejectionReason,
        decimal inspectionFee,
        string diagnosticFindings,
        IEnumerable<(
            QuoteItemType ItemType,
            string Description,
            int Quantity,
            decimal UnitPrice)> items)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            complaintId);

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            technicianId);

        ArgumentNullException.ThrowIfNull(items);

        QuoteItem.ValidateMoney(
            inspectionFee,
            nameof(inspectionFee));

        if (string.IsNullOrWhiteSpace(diagnosticFindings) ||
            diagnosticFindings.Trim().Length > 2000)
        {
            throw new ArgumentException(
                "Diagnostic findings of up to 2000 characters are required.",
                nameof(diagnosticFindings));
        }

        var warrantyReason =
            string.IsNullOrWhiteSpace(warrantyRejectionReason)
                ? null
                : warrantyRejectionReason.Trim();

        if (warrantyReason?.Length > 500)
        {
            throw new ArgumentException(
                "Warranty rejection reason cannot exceed 500 characters.",
                nameof(warrantyRejectionReason));
        }

        if (isWarrantyValid)
        {
            if (warrantyReason is not null)
            {
                throw new ArgumentException(
                    "A valid warranty cannot have a rejection reason.",
                    nameof(warrantyRejectionReason));
            }

            if (inspectionFee != 0m)
            {
                throw new ArgumentException(
                    "Warranty-covered repairs cannot have an inspection fee.",
                    nameof(inspectionFee));
            }
        }
        else
        {
            if (warrantyReason is null)
            {
                throw new ArgumentException(
                    "A warranty rejection reason is mandatory.",
                    nameof(warrantyRejectionReason));
            }

            if (inspectionFee <= 0m)
            {
                throw new ArgumentException(
                    "An out-of-warranty inspection fee is required.",
                    nameof(inspectionFee));
            }
        }

        var quoteItems = items
            .Take(101)
            .Select(item => QuoteItem.Create(
                item.ItemType,
                item.Description,
                item.Quantity,
                item.UnitPrice))
            .ToList();

        if (quoteItems.Count is < 1 or > 100)
        {
            throw new ArgumentException(
                "A quotation must contain between 1 and 100 items.",
                nameof(items));
        }

        var total = quoteItems.Sum(item => item.TotalPrice);

        QuoteItem.ValidateMoney(
            total,
            nameof(EstimatedTotalCost));

        if (isWarrantyValid && total != 0m)
        {
            throw new ArgumentException(
                "Fully warranty-covered repairs must have a zero total.",
                nameof(items));
        }

        var quote = new Quote
        {
            ComplaintId = complaintId,
            TechnicianId = technicianId,

            DiagnosticFindings = diagnosticFindings.Trim(),

            IsWarrantyValid = isWarrantyValid,
            WarrantyRejectionReason = warrantyReason,

            InspectionFee = inspectionFee,
            EstimatedTotalCost = total,

            CustomerDecision = QuoteDecision.Pending,
            CreatedAt = DateTime.UtcNow
        };

        quote._items.AddRange(quoteItems);

        return quote;
    }

    public void RecordDecision(
        QuoteDecision decision,
        string? reason = null)
    {
        if (CustomerDecision != QuoteDecision.Pending)
        {
            throw new InvalidOperationException(
                "A decision has already been recorded.");
        }

        if (decision is not (
            QuoteDecision.Accepted or QuoteDecision.Rejected))
        {
            throw new ArgumentOutOfRangeException(
                nameof(decision));
        }

        var normalizedReason =
            string.IsNullOrWhiteSpace(reason)
                ? null
                : reason.Trim();

        if (normalizedReason?.Length > 500)
        {
            throw new ArgumentException(
                "Decision reason cannot exceed 500 characters.",
                nameof(reason));
        }

        CustomerDecision = decision;
        DecisionReason = normalizedReason;
        DecisionTimestamp = DateTime.UtcNow;
    }
}
