namespace RepairSystem.Application.Contracts.Complaints;

public enum ComplaintAssignmentResult
{
    Assigned,
    ComplaintNotFound,
    TechnicianNotFound,
    TechnicianInactive,
    UserIsNotTechnician,
    AlreadyAssigned,
    InvalidComplaintStatus
}