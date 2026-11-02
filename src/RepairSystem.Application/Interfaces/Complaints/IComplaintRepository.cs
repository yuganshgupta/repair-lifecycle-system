using RepairSystem.Domain.Entities;

namespace RepairSystem.Application.Interfaces.Complaints;

public interface IComplaintRepository
{
    Task<IReadOnlyList<Complaint>> GetByCustomerIdAsync(
        int customerId);

    Task AddAsync(Complaint complaint);

    Task<IReadOnlyList<Complaint>> GetAssignedToTechnicianAsync(
    int technicianId);
}