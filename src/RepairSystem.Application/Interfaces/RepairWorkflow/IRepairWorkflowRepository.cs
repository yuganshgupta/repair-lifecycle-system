
using RepairSystem.Domain.Entities;

namespace RepairSystem.Application.Interfaces.RepairWorkflow;

public interface IRepairWorkflowRepository
{
    Task<User?> GetUserAsync(
        int userId, CancellationToken ct);

    Task<Complaint?> GetComplaintAsync(
        int complaintId, CancellationToken ct);

    Task<Quote?> GetQuoteAsync(
        int complaintId, CancellationToken ct);

    Task<IReadOnlyList<Attachment>> GetAttachmentsAsync(
        int complaintId, CancellationToken ct);

    Task SaveQuoteChangeAsync(
        Quote quote,
        bool isNew,
        ComplaintStatusHistory history,
        BusinessActivityEvent activity,
        CancellationToken ct);

    Task AddAttachmentAsync(
        Attachment attachment, CancellationToken ct);
}
