using Microsoft.EntityFrameworkCore;
using RepairSystem.Application.Interfaces.Complaints;
using RepairSystem.Domain.Entities;
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
}