using Microsoft.EntityFrameworkCore;
using RepairSystem.Application.Contracts.Assignments;
using RepairSystem.Application.Interfaces.Assignments;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;
using RepairSystem.Infrastructure.Data;

namespace RepairSystem.Infrastructure.Repositories.Assignments;

public sealed class TicketAssignmentRepository(RepairDbContext context) : ITicketAssignmentRepository
{
    public Task<User?> GetUserAsync(int id, CancellationToken ct) =>
        context.Users.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == id, ct);

    // Tracked: the original SQL Server rowversion participates in the UPDATE.
    public Task<Complaint?> GetComplaintAsync(int id, CancellationToken ct) =>
        context.Complaints.SingleOrDefaultAsync(x => x.ComplaintId == id, ct);

    public async Task<IReadOnlyList<AssignmentTechnicianDto>> GetTechniciansAsync(CancellationToken ct) =>
        await context.Users.AsNoTracking()
            .Where(x => x.Role == UserRole.Technician && x.IsActive)
            .OrderBy(x => x.FullName).ThenBy(x => x.UserId)
            .Select(x => new AssignmentTechnicianDto(x.UserId, x.FullName)).ToListAsync(ct);

    public async Task<IReadOnlyList<AssignmentTicketDto>> GetBoardAsync(int? technicianId, CancellationToken ct)
    {
        var query = context.Complaints.AsNoTracking();
        if (technicianId.HasValue)
        {
            var id = technicianId.Value;
            query = query.Where(x =>
                (x.AssignedTechnicianId == null && x.Status == ComplaintStatus.Submitted) ||
                (x.AssignedTechnicianId == id && (x.Status == ComplaintStatus.Assigned ||
                    x.Status == ComplaintStatus.InDiagnosis || x.Status == ComplaintStatus.QuotePending ||
                    x.Status == ComplaintStatus.Approved || x.Status == ComplaintStatus.InRepair)));
        }
        else
        {
            query = query.Where(x => x.AssignedTechnicianId != null &&
                (x.Status == ComplaintStatus.Assigned || x.Status == ComplaintStatus.InDiagnosis ||
                x.Status == ComplaintStatus.QuotePending || x.Status == ComplaintStatus.Approved || x.Status == ComplaintStatus.InRepair));
        }

        var rows = await query.OrderBy(x => x.CreatedAt).ThenBy(x => x.ComplaintId)
            .Select(x => new { x.ComplaintId, x.TicketNumber, x.DeviceType, x.DeviceBrand,
                x.DeviceModel, x.Status, x.AssignedTechnicianId,
                TechnicianName = x.AssignedTechnician == null ? null : x.AssignedTechnician.FullName })
            .ToListAsync(ct);

        return rows.Select(x => new AssignmentTicketDto(x.ComplaintId, x.TicketNumber,
            $"{x.DeviceBrand} {x.DeviceModel} ({x.DeviceType})", x.Status.ToString(),
            x.AssignedTechnicianId, x.TechnicianName)).ToList();
    }

    public async Task SaveAsync(ComplaintStatusHistory? history, BusinessActivityEvent activity, CancellationToken ct)
    {
        if (history is not null) context.ComplaintStatusHistories.Add(history);
        context.BusinessActivityEvents.Add(activity);
        // All writes commit together; concurrency exceptions roll back the entire save.
        await context.SaveChangesAsync(ct);
    }
}
