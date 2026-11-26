
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RepairSystem.Application.Contracts.RepairWorkflow;
using RepairSystem.Application.Interfaces.RepairWorkflow;
using RepairSystem.Domain.Entities;
using RepairSystem.Infrastructure.Data;

namespace RepairSystem.Infrastructure.Repositories.RepairWorkflow;

public sealed class RepairWorkflowRepository
    : IRepairWorkflowRepository
{
    private readonly RepairDbContext _context;

    public RepairWorkflowRepository(RepairDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetUserAsync(
        int userId, CancellationToken ct)
    {
        return _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.UserId == userId, ct);
    }

    public Task<Complaint?> GetComplaintAsync(
        int complaintId, CancellationToken ct)
    {
        return _context.Complaints
            .SingleOrDefaultAsync(
                x => x.ComplaintId == complaintId, ct);
    }

    public Task<Quote?> GetQuoteAsync(
        int complaintId, CancellationToken ct)
    {
        return _context.Quotes
            .Include(x => x.Items)
            .SingleOrDefaultAsync(
                x => x.ComplaintId == complaintId, ct);
    }

    public async Task<IReadOnlyList<Attachment>>
        GetAttachmentsAsync(
            int complaintId, CancellationToken ct)
    {
        return await _context.Attachments
            .AsNoTracking()
            .Where(x => x.ComplaintId == complaintId)
            .OrderBy(x => x.UploadedAt)
            .ThenBy(x => x.AttachmentId)
            .ToListAsync(ct);
    }

    public async Task SaveQuoteChangeAsync(
        Quote quote,
        bool isNew,
        ComplaintStatusHistory history,
        BusinessActivityEvent activity,
        CancellationToken ct)
    {
        if (isNew)
        {
            _context.Quotes.Add(quote);
        }

        _context.ComplaintStatusHistories.Add(history);
        _context.BusinessActivityEvents.Add(activity);

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new WorkflowException(
                WorkflowError.Conflict,
                "The quotation has changed. Refresh and try again.");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException
            { Number: 2601 or 2627 })
        {
            throw new WorkflowException(
                WorkflowError.Conflict,
                "A quotation already exists for this complaint.");
        }
    }

    public async Task AddAttachmentAsync(
        Attachment attachment, CancellationToken ct)
    {
        _context.Attachments.Add(attachment);

        await _context.SaveChangesAsync(ct);
    }
}
