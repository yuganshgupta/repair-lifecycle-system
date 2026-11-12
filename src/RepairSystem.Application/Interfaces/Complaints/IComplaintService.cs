using RepairSystem.Application.Contracts.Complaints;

namespace RepairSystem.Application.Interfaces.Complaints;

public interface IComplaintService
{
    Task<IReadOnlyList<CustomerComplaintDto>>
        GetCustomerComplaintsAsync(int customerId);

    Task<CustomerComplaintDto> CreateCustomerComplaintAsync(
        int customerId,
        CreateComplaintRequestDto request);

    Task<IReadOnlyList<TechnicianComplaintDto>>
    GetTechnicianComplaintsAsync(int technicianId);

    Task<ComplaintAssignmentResult> AssignTechnicianAsync(
    int complaintId,
    int technicianId,
    int assignedByAdminId);

    Task<IReadOnlyList<AdminUnassignedComplaintDto>>
    GetUnassignedComplaintsAsync();

    Task<TechnicianStatusUpdateResult> UpdateTechnicianComplaintStatusAsync(
    int complaintId,
    int technicianId,
    RepairSystem.Domain.Enums.ComplaintStatus newStatus);
}