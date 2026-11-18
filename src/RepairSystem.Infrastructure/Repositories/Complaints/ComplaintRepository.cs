using Microsoft.EntityFrameworkCore;
using RepairSystem.Application.Interfaces.Complaints;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;
using RepairSystem.Infrastructure.Data;

namespace RepairSystem.Infrastructure.Repositories.Complaints;

public sealed class ComplaintRepository : IComplaintRepository
{
    private readonly RepairDbContext _context;

    public ComplaintRepository(RepairDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Complaint>> GetByCustomerIdAsync(
        int customerId)
    {
        return await _context.Complaints
            .AsNoTracking()
            .Where(complaint => complaint.CustomerId == customerId)
            .OrderByDescending(complaint => complaint.CreatedAt)
            .ToListAsync();
    }

    public async Task AddAsync(Complaint complaint)
    {
        _context.Complaints.Add(complaint);

        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Complaint>> GetAssignedToTechnicianAsync(
    int technicianId)
    {
        return await _context.Complaints
            .AsNoTracking()
            .Where(complaint =>
                complaint.AssignedTechnicianId == technicianId)
            .Include(complaint => complaint.Customer)
            .OrderByDescending(complaint => complaint.CreatedAt)
            .ToListAsync();
    }

    public async Task<Complaint?> GetByIdAsync(int complaintId)
    {
        return await _context.Complaints
            .SingleOrDefaultAsync(
                complaint => complaint.ComplaintId == complaintId);
    }

    public async Task SaveAssignmentAsync(
    Complaint complaint,
    ComplaintStatusHistory history,
    BusinessActivityEvent activity)
    {
        ArgumentNullException.ThrowIfNull(complaint);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(activity);

        // The complaint is already tracked by EF Core.
        _context.ComplaintStatusHistories.Add(history);

        // Register the new business activity event.
        _context.BusinessActivityEvents.Add(activity);

        // Persist all three changes together.
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Complaint>> GetUnassignedAsync()
    {
        return await _context.Complaints
            .AsNoTracking()
            .Where(complaint =>
                complaint.AssignedTechnicianId == null &&
                complaint.Status == ComplaintStatus.Submitted)
            .Include(complaint => complaint.Customer)
            .OrderBy(complaint => complaint.CreatedAt)
            .ToListAsync();
    }

    public async Task SaveStatusChangeAsync(
    Complaint complaint,
    ComplaintStatusHistory history,
    BusinessActivityEvent activity)
    {
        ArgumentNullException.ThrowIfNull(complaint);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(activity);

        _context.ComplaintStatusHistories.Add(history);
        _context.BusinessActivityEvents.Add(activity);

        // The complaint is already tracked.
        // Save all three changes in one transaction.
        await _context.SaveChangesAsync();
    }

    public async Task AddWithHistoryAsync(
    Complaint complaint,
    ComplaintStatusHistory history)
    {
        history.Complaint = complaint;

        _context.Complaints.Add(complaint);
        _context.ComplaintStatusHistories.Add(history);

        await _context.SaveChangesAsync();
    }

    public async Task AddWithHistoryAndActivityAsync(
    Complaint complaint,
    ComplaintStatusHistory history,
    Func<Complaint, BusinessActivityEvent> createActivity)
    {
        ArgumentNullException.ThrowIfNull(complaint);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(createActivity);

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        history.Complaint = complaint;

        _context.Complaints.Add(complaint);
        _context.ComplaintStatusHistories.Add(history);

        // First save generates the ComplaintId.
        await _context.SaveChangesAsync();

        // Now create an activity containing the generated ID.
        var activity = createActivity(complaint);

        _context.BusinessActivityEvents.Add(activity);

        // Save the activity within the same transaction.
        await _context.SaveChangesAsync();

        // Commit only after every database write succeeds.
        await transaction.CommitAsync();
    }

    public async Task<IReadOnlyList<ComplaintStatusHistory>>
        GetStatusHistoryByComplaintIdAsync(int complaintId)
    {
        return await _context.ComplaintStatusHistories
            .AsNoTracking()
            .Where(history => history.ComplaintId == complaintId)
            .Include(history => history.ChangedByUser)
            .OrderBy(history => history.ChangedAt)
            .ThenBy(history => history.ComplaintStatusHistoryId)
            .ToListAsync();
    }
}