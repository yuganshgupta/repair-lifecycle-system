
namespace RepairSystem.Application.Contracts.Complaints;

public enum TechnicianStatusUpdateResult
{
    Updated,
    ComplaintNotFound,
    ComplaintNotAssignedToTechnician,
    InvalidStatus,
    InvalidTransition
}
