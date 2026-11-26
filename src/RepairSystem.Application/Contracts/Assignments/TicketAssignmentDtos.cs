namespace RepairSystem.Application.Contracts.Assignments;

public sealed record AssignmentTicketDto(
    int ComplaintId, string TicketNumber, string Device,
    string Status, int? AssignedTechnicianId, string? AssignedTechnicianName);

public sealed record AssignmentTechnicianDto(int UserId, string FullName);

public sealed record AssignmentBoardDto(
    bool IsAdmin,
    IReadOnlyList<AssignmentTicketDto> Tickets,
    IReadOnlyList<AssignmentTechnicianDto> Technicians);

public sealed record MoveAssignmentRequest(
    int TechnicianId, int? ExpectedTechnicianId, string? Reason);
