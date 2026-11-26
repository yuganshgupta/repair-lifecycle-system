
using RepairSystem.Application.Contracts.RepairWorkflow;
using RepairSystem.Application.Interfaces.RepairWorkflow;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Application.Services.RepairWorkflow;

public sealed class RepairWorkflowService
{
    private const long MaxFileBytes = 20L * 1024 * 1024;

    private readonly IRepairWorkflowRepository _repository;

    public RepairWorkflowService(
        IRepairWorkflowRepository repository)
    {
        _repository = repository;
    }

    public async Task<RepairWorkflowDto> GetAsync(
        int complaintId,
        int userId,
        decimal inspectionFee,
        CancellationToken ct)
    {
        var (_, complaint) = await GetContextAsync(
            complaintId, userId, ct);

        var quote = await _repository.GetQuoteAsync(
            complaintId, ct);

        var attachments = await _repository
            .GetAttachmentsAsync(complaintId, ct);

        return new RepairWorkflowDto(
            complaintId,
            complaint.TicketNumber,
            complaint.Status.ToString(),
            inspectionFee,
            quote is null ? null : MapQuote(quote),
            attachments.Select(MapEvidence).ToList());
    }

    public async Task<QuoteDto> SubmitQuoteAsync(
        int complaintId,
        int userId,
        SubmitQuoteRequest request,
        decimal configuredInspectionFee,
        CancellationToken ct)
    {
        var (actor, complaint) = await GetContextAsync(
            complaintId, userId, ct);

        RequireRole(actor, UserRole.Technician);

        if (complaint.Status != ComplaintStatus.InDiagnosis)
        {
            throw new WorkflowException(
                WorkflowError.Conflict,
                "A quotation can only be submitted during diagnosis.");
        }

        if (await _repository.GetQuoteAsync(complaintId, ct)
            is not null)
        {
            throw new WorkflowException(
                WorkflowError.Conflict,
                "This complaint already has a quotation.");
        }

        if (request.Items is null ||
            request.Items.Count is < 1 or > 100 ||
            request.Items.Any(x => x is null))
        {
            throw new WorkflowException(
                WorkflowError.Invalid,
                "Provide between 1 and 100 valid quotation items.");
        }

        if (!request.IsWarrantyValid)
        {
            var attachments = await _repository
                .GetAttachmentsAsync(complaintId, ct);

            var hasProof = attachments.Any(x =>
                x.Purpose ==
                    AttachmentPurpose.WarrantyRejectionProof &&
                x.UploadedByUserId == userId);

            if (!hasProof)
            {
                throw new WorkflowException(
                    WorkflowError.Invalid,
                    "Upload warranty rejection evidence before submitting.");
            }

            if (configuredInspectionFee <= 0m)
            {
                throw new WorkflowException(
                    WorkflowError.Conflict,
                    "An inspection fee must be configured first.");
            }
        }

        var quote = Quote.Create(
            complaintId,
            userId,
            request.IsWarrantyValid,
            request.WarrantyRejectionReason,
            request.IsWarrantyValid
                ? 0m
                : configuredInspectionFee,
            request.DiagnosticFindings,
            request.Items.Select(x => (
                (QuoteItemType)x.ItemType,
                x.Description,
                x.Quantity,
                x.UnitPrice)));

        var history = ChangeStatus(
            complaint,
            actor.UserId,
            ComplaintStatus.QuotePending);

        var activity = CreateActivity(
            complaint,
            actor,
            BusinessActivityType.QuotationSubmitted,
            "Quotation submitted");

        await _repository.SaveQuoteChangeAsync(
            quote,
            true,
            history,
            activity,
            ct);

        return MapQuote(quote);
    }

    public async Task<QuoteDto> DecideQuoteAsync(
        int complaintId,
        int userId,
        QuoteDecisionRequest request,
        CancellationToken ct)
    {
        var (actor, complaint) = await GetContextAsync(
            complaintId, userId, ct);

        RequireRole(actor, UserRole.Customer);

        if (request.Accept is null)
        {
            throw new WorkflowException(
                WorkflowError.Invalid,
                "An explicit acceptance or rejection is required.");
        }

        if (complaint.Status != ComplaintStatus.QuotePending)
        {
            throw new WorkflowException(
                WorkflowError.Conflict,
                "This complaint is not awaiting a quotation decision.");
        }

        var quote = await _repository.GetQuoteAsync(
            complaintId, ct);

        if (quote is null)
        {
            throw new WorkflowException(
                WorkflowError.NotFound,
                "Quotation not found.");
        }

        if (quote.CustomerDecision != QuoteDecision.Pending)
        {
            throw new WorkflowException(
                WorkflowError.Conflict,
                "A decision has already been recorded.");
        }

        var accepted = request.Accept.Value;

        quote.RecordDecision(
            accepted
                ? QuoteDecision.Accepted
                : QuoteDecision.Rejected,
            request.Reason);

        var history = ChangeStatus(
            complaint,
            userId,
            accepted
                ? ComplaintStatus.Approved
                : ComplaintStatus.Cancelled);

        var activity = CreateActivity(
            complaint,
            actor,
            accepted
                ? BusinessActivityType.QuotationAccepted
                : BusinessActivityType.QuotationRejected,
            accepted
                ? "Quotation accepted"
                : "Quotation rejected");

        await _repository.SaveQuoteChangeAsync(
            quote,
            false,
            history,
            activity,
            ct);

        return MapQuote(quote);
    }

    public async Task<Attachment> PrepareAttachmentAsync(
        int complaintId,
        int userId,
        int purpose,
        string fileKey,
        string contentType,
        long size,
        CancellationToken ct)
    {
        var (actor, complaint) = await GetContextAsync(
            complaintId, userId, ct);

        if (!Enum.IsDefined((AttachmentPurpose)purpose))
        {
            throw new WorkflowException(
                WorkflowError.Invalid,
                "Invalid attachment purpose.");
        }

        if (size <= 0 || size > MaxFileBytes)
        {
            throw new WorkflowException(
                WorkflowError.Invalid,
                "The file must be no larger than 20 MB.");
        }

        if (contentType is not ("image/jpeg" or "image/png"))
        {
            throw new WorkflowException(
                WorkflowError.Invalid,
                "Only JPEG and PNG images are supported.");
        }

        var attachmentPurpose = (AttachmentPurpose)purpose;

        var allowed = attachmentPurpose switch
        {
            AttachmentPurpose.CustomerIssueProof =>
                actor.Role == UserRole.Customer &&
                complaint.Status is
                    ComplaintStatus.Submitted or
                    ComplaintStatus.Assigned or
                    ComplaintStatus.InDiagnosis,

            AttachmentPurpose.WarrantyRejectionProof =>
                actor.Role == UserRole.Technician &&
                complaint.Status == ComplaintStatus.InDiagnosis,

            AttachmentPurpose.RepairProgressProof =>
                actor.Role == UserRole.Technician &&
                complaint.Status is
                    ComplaintStatus.InDiagnosis or
                    ComplaintStatus.Approved or
                    ComplaintStatus.InRepair or
                    ComplaintStatus.Completed,

            _ => false
        };

        if (!allowed)
        {
            throw new WorkflowException(
                WorkflowError.Conflict,
                "This attachment type is not allowed at the current stage.");
        }

        return new Attachment
        {
            ComplaintId = complaintId,
            UploadedByUserId = userId,
            Purpose = attachmentPurpose,
            FilePath = fileKey,
            FileSizeBytes = size,
            ContentType = contentType,
            UploadedAt = DateTime.UtcNow
        };
    }

    public async Task<EvidenceDto> SaveAttachmentAsync(
        Attachment attachment,
        CancellationToken ct)
    {
        await _repository.AddAttachmentAsync(attachment, ct);

        return MapEvidence(attachment);
    }

    public async Task<Attachment> GetAttachmentAsync(
        int complaintId,
        int attachmentId,
        int userId,
        CancellationToken ct)
    {
        await GetContextAsync(complaintId, userId, ct);

        var attachments = await _repository
            .GetAttachmentsAsync(complaintId, ct);

        return attachments.SingleOrDefault(
            x => x.AttachmentId == attachmentId)
            ?? throw new WorkflowException(
                WorkflowError.NotFound,
                "Attachment not found.");
    }

    private async Task<(User Actor, Complaint Complaint)>
        GetContextAsync(
            int complaintId,
            int userId,
            CancellationToken ct)
    {
        var actor = await _repository.GetUserAsync(userId, ct);

        if (actor is null || !actor.IsActive)
        {
            throw new WorkflowException(
                WorkflowError.Forbidden,
                "Your account is not active.");
        }

        var complaint = await _repository.GetComplaintAsync(
            complaintId, ct);

        if (complaint is null)
        {
            throw new WorkflowException(
                WorkflowError.NotFound,
                "Complaint not found.");
        }

        var allowed = actor.Role switch
        {
            UserRole.Admin => true,

            UserRole.Customer =>
                complaint.CustomerId == userId,

            UserRole.Technician =>
                complaint.AssignedTechnicianId == userId,

            _ => false
        };

        if (!allowed)
        {
            throw new WorkflowException(
                WorkflowError.NotFound,
                "Complaint not found.");
        }

        return (actor, complaint);
    }

    private static void RequireRole(
        User actor, UserRole requiredRole)
    {
        if (actor.Role != requiredRole)
        {
            throw new WorkflowException(
                WorkflowError.Forbidden,
                "You cannot perform this operation.");
        }
    }

    private static ComplaintStatusHistory ChangeStatus(
        Complaint complaint,
        int actorId,
        ComplaintStatus newStatus)
    {
        var previousStatus = complaint.Status;

        complaint.ChangeStatus(newStatus);

        return new ComplaintStatusHistory
        {
            ComplaintId = complaint.ComplaintId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = actorId,
            ChangedAt = complaint.UpdatedAt
        };
    }

    private static BusinessActivityEvent CreateActivity(
        Complaint complaint,
        User actor,
        BusinessActivityType eventType,
        string title)
    {
        var actorName = string.IsNullOrWhiteSpace(actor.FullName)
            ? $"User #{actor.UserId}"
            : actor.FullName.Trim();

        actorName = actorName[..Math.Min(actorName.Length, 120)];

        return BusinessActivityEvent.Create(
            eventType: eventType,
            actorName: actorName,
            actorRole: actor.Role.ToString(),
            title: title,
            description: $"{title} for {complaint.TicketNumber}.",
            actorUserId: actor.UserId,
            customerId: complaint.CustomerId,
            complaintId: complaint.ComplaintId,
            ticketNumber: complaint.TicketNumber);
    }

    private static EvidenceDto MapEvidence(Attachment x)
    {
        return new EvidenceDto(
            x.AttachmentId,
            x.Purpose.ToString(),
            x.FileSizeBytes,
            x.UploadedAt);
    }

    private static QuoteDto MapQuote(Quote quote)
    {
        return new QuoteDto(
            quote.QuoteId,
            quote.DiagnosticFindings,
            quote.IsWarrantyValid,
            quote.WarrantyRejectionReason,
            quote.InspectionFee,
            quote.InspectionFeeDue,
            quote.EstimatedTotalCost,
            quote.CustomerDecision.ToString(),
            quote.DecisionReason,
            quote.DecisionTimestamp,
            quote.Items.Select(x => new QuoteLineDto(
                x.ItemType.ToString(),
                x.Description,
                x.Quantity,
                x.UnitPrice,
                x.TotalPrice)).ToList());
    }
}
