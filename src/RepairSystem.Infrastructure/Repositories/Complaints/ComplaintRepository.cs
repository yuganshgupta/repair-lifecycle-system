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
    ComplaintStatusHistory history)
    {
        _context.ComplaintStatusHistories.Add(history);

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
}