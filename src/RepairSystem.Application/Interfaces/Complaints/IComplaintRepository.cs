using RepairSystem.Domain.Entities;

namespace RepairSystem.Application.Interfaces.Complaints;

public interface IComplaintRepository
{
    Task<IReadOnlyList<Complaint>> GetByCustomerIdAsync(
        int customerId);

    Task AddAsync(Complaint complaint);

    Task<IReadOnlyList<Complaint>> GetAssignedToTechnicianAsync(
    int technicianId);

    Task<Complaint?> GetByIdAsync(int complaintId);

    Task SaveAssignmentAsync(
        Complaint complaint,
        ComplaintStatusHistory history);

    Task<IReadOnlyList<Complaint>> GetUnassignedAsync();

    Task SaveStatusChangeAsync(
    Complaint complaint,
    ComplaintStatusHistory history);
}