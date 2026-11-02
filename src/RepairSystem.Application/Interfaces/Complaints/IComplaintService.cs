using RepairSystem.Application.Contracts.Complaints;

namespace RepairSystem.Application.Interfaces.Complaints;

public interface IComplaintService
{
    Task<IReadOnlyList<CustomerComplaintDto>>
        GetCustomerComplaintsAsync(int customerId);

    Task<CustomerComplaintDto> CreateCustomerComplaintAsync(
        int customerId,
        CreateComplaintRequestDto request);
}