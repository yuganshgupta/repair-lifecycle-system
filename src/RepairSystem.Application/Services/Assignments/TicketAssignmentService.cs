using System.Text.Json;
using RepairSystem.Application.Contracts.Assignments;
using RepairSystem.Application.Contracts.RepairWorkflow;
using RepairSystem.Application.Interfaces.Assignments;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Application.Services.Assignments;

public sealed class TicketAssignmentService(ITicketAssignmentRepository repository)
{
    public async Task<AssignmentBoardDto> GetBoardAsync(int actorId, CancellationToken ct)
    {
        var actor = await GetActorAsync(actorId, ct);
        var admin = actor.Role == UserRole.Admin;
        var tickets = await repository.GetBoardAsync(admin ? null : actorId, ct);
        var technicians = await repository.GetTechniciansAsync(ct);
        return new AssignmentBoardDto(admin, tickets, technicians);
    }

    public async Task ClaimAsync(int complaintId, int actorId, CancellationToken ct)
    {
        var actor = await GetActorAsync(actorId, ct);
        if (actor.Role != UserRole.Technician)
            throw new WorkflowException(WorkflowError.Forbidden, "Only technicians may claim tickets.");

        var complaint = await GetComplaintAsync(complaintId, ct);
        if (complaint.Status != ComplaintStatus.Submitted || complaint.AssignedTechnicianId.HasValue)
            throw new WorkflowException(WorkflowError.Conflict, "This ticket is no longer available.");

        var activity = CreateActivity(complaint, actor, actor,
            BusinessActivityType.TechnicianClaimed, "Ticket claimed", null, null);

        complaint.AssignToTechnician(actorId);
        var history = new ComplaintStatusHistory
        {
            ComplaintId = complaint.ComplaintId,
            PreviousStatus = ComplaintStatus.Submitted,
            NewStatus = ComplaintStatus.Assigned,
            ChangedByUserId = actorId,
            ChangedAt = complaint.UpdatedAt
        };
        await repository.SaveAsync(history, activity, ct);
    }

    public async Task MoveAsync(int complaintId, int actorId, MoveAssignmentRequest request, CancellationToken ct)
    {
        var actor = await GetActorAsync(actorId, ct);
        var complaint = await GetComplaintAsync(complaintId, ct);
        var admin = actor.Role == UserRole.Admin;
        if (!admin && complaint.AssignedTechnicianId != actorId)
            throw new WorkflowException(WorkflowError.NotFound, "Complaint not found.");

        if (!complaint.AssignedTechnicianId.HasValue ||
            complaint.Status is not (ComplaintStatus.Assigned or ComplaintStatus.InDiagnosis or
                ComplaintStatus.QuotePending or ComplaintStatus.Approved or ComplaintStatus.InRepair))
            throw new WorkflowException(WorkflowError.Conflict, "Only active assigned complaints can be moved.");

        if (request.ExpectedTechnicianId != complaint.AssignedTechnicianId)
            throw new WorkflowException(WorkflowError.Conflict, "Assignment changed. Refresh before trying again.");

        if (request.TechnicianId <= 0 || request.TechnicianId == complaint.AssignedTechnicianId)
            throw new WorkflowException(WorkflowError.Invalid, "Choose a different technician.");

        var reason = request.Reason?.Trim();
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)
            throw new WorkflowException(WorkflowError.Invalid, "A transfer reason of 1 to 500 characters is required.");

        var target = await repository.GetUserAsync(request.TechnicianId, ct);
        if (target is null || target.Role != UserRole.Technician || !target.IsActive)
            throw new WorkflowException(WorkflowError.Invalid, "Choose an active technician.");

        var oldId = complaint.AssignedTechnicianId.Value;
        var activity = CreateActivity(complaint, actor, target,
            admin ? BusinessActivityType.TechnicianReassigned : BusinessActivityType.TechnicianTransferred,
            admin ? "Administrator reassigned ticket" : "Technician transferred ticket", oldId, reason);

        complaint.ReassignToTechnician(target.UserId);
        // No fake status transition: assignment changes are in BusinessActivityEvents.
        await repository.SaveAsync(null, activity, ct);
    }

    private async Task<User> GetActorAsync(int id, CancellationToken ct)
    {
        var actor = await repository.GetUserAsync(id, ct);
        if (actor is null || !actor.IsActive || actor.Role is not (UserRole.Admin or UserRole.Technician))
            throw new WorkflowException(WorkflowError.Forbidden, "Your account cannot manage assignments.");
        return actor;
    }

    private async Task<Complaint> GetComplaintAsync(int id, CancellationToken ct) =>
        await repository.GetComplaintAsync(id, ct) ??
            throw new WorkflowException(WorkflowError.NotFound, "Complaint not found.");

    private static string Name(User user)
    {
        var name = string.IsNullOrWhiteSpace(user.FullName) ? $"User #{user.UserId}" : user.FullName.Trim();
        return name[..Math.Min(name.Length, 120)];
    }

    private static BusinessActivityEvent CreateActivity(Complaint complaint, User actor, User target,
        BusinessActivityType type, string title, int? oldId, string? reason) =>
        BusinessActivityEvent.Create(
            eventType: type, actorName: Name(actor), actorRole: actor.Role.ToString(),
            title: title,
            description: oldId.HasValue
                ? $"{complaint.TicketNumber}: technician #{oldId} -> {Name(target)} (#{target.UserId}). Reason: {reason}"
                : $"{Name(actor)} claimed {complaint.TicketNumber}.",
            actorUserId: actor.UserId, targetUserId: target.UserId, targetUserName: Name(target),
            customerId: complaint.CustomerId, complaintId: complaint.ComplaintId,
            ticketNumber: complaint.TicketNumber,
            detailsJson: JsonSerializer.Serialize(new { PreviousTechnicianId = oldId,
                NewTechnicianId = target.UserId, Reason = reason, StatusBefore = complaint.Status.ToString() }));
}
