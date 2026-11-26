using RepairSystem.Application.Contracts.Assignments;
using RepairSystem.Domain.Entities;

namespace RepairSystem.Application.Interfaces.Assignments;

public interface ITicketAssignmentRepository
{
    Task<User?> GetUserAsync(int id, CancellationToken ct);
    Task<Complaint?> GetComplaintAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<AssignmentTechnicianDto>> GetTechniciansAsync(CancellationToken ct);
    // Null technicianId means Admin; otherwise only available or owned tickets.
    Task<IReadOnlyList<AssignmentTicketDto>> GetBoardAsync(int? technicianId, CancellationToken ct);
    Task SaveAsync(ComplaintStatusHistory? history, BusinessActivityEvent activity, CancellationToken ct);
}
