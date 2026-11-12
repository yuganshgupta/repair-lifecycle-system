
using RepairSystem.Domain.Enums;

namespace RepairSystem.Application.Contracts.Complaints;

public sealed class UpdateTechnicianComplaintStatusRequestDto
{
    public ComplaintStatus NewStatus { get; set; }
}
