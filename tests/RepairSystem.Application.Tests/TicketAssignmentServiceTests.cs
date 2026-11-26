using Microsoft.VisualStudio.TestTools.UnitTesting;
using RepairSystem.Application.Contracts.Assignments;
using RepairSystem.Application.Contracts.RepairWorkflow;
using RepairSystem.Application.Interfaces.Assignments;
using RepairSystem.Application.Services.Assignments;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Application.Tests;

[TestClass]
public class TicketAssignmentServiceTests
{
    private static readonly CancellationToken Ct = CancellationToken.None;

    [TestMethod]
    public async Task Claim_ChangesOwnerStatusAndHistory()
    {
        var repo = new Fake();
        await new TicketAssignmentService(repo).ClaimAsync(101, 20, Ct);
        Assert.AreEqual<int?>(20, repo.Ticket.AssignedTechnicianId);
        Assert.AreEqual(ComplaintStatus.Assigned, repo.Ticket.Status);
        Assert.AreEqual(ComplaintStatus.Assigned, repo.History!.NewStatus);
        Assert.AreEqual(BusinessActivityType.TechnicianClaimed, repo.Activity!.EventType);
        Assert.AreEqual(1, repo.SaveCount);
    }

    [TestMethod]
    public async Task Claim_AlreadyAssigned_IsRejected()
    {
        var repo = new Fake(ComplaintStatus.Assigned);
        var ex = await Assert.ThrowsExactlyAsync<WorkflowException>(() => new TicketAssignmentService(repo).ClaimAsync(101, 21, Ct));
        Assert.AreEqual(WorkflowError.Conflict, ex.Error);
        Assert.AreEqual(0, repo.SaveCount);
    }

    [TestMethod]
    public async Task Transfer_PreservesRepairStatusAndRecordsReason()
    {
        var repo = new Fake(ComplaintStatus.InRepair);
        await new TicketAssignmentService(repo).MoveAsync(101, 20, new(21, 20, "Specialist required"), Ct);
        Assert.AreEqual<int?>(21, repo.Ticket.AssignedTechnicianId);
        Assert.AreEqual(ComplaintStatus.InRepair, repo.Ticket.Status);
        Assert.IsNull(repo.History);
        Assert.AreEqual(BusinessActivityType.TechnicianTransferred, repo.Activity!.EventType);
        StringAssert.Contains(repo.Activity.Description, "Specialist required");
    }

    [TestMethod]
    public async Task AdminOverride_PreservesQuotePendingStatus()
    {
        var repo = new Fake(ComplaintStatus.QuotePending);
        await new TicketAssignmentService(repo).MoveAsync(101, 30, new(21, 20, "Workload"), Ct);
        Assert.AreEqual<int?>(21, repo.Ticket.AssignedTechnicianId);
        Assert.AreEqual(ComplaintStatus.QuotePending, repo.Ticket.Status);
        Assert.AreEqual(BusinessActivityType.TechnicianReassigned, repo.Activity!.EventType);
    }

    [TestMethod]
    public async Task OtherTechnician_CannotTransferTicket()
    {
        var repo = new Fake(ComplaintStatus.InDiagnosis);
        var ex = await Assert.ThrowsExactlyAsync<WorkflowException>(() => new TicketAssignmentService(repo).MoveAsync(101, 21, new(21, 20, "Not mine"), Ct));
        Assert.AreEqual(WorkflowError.NotFound, ex.Error);
        Assert.AreEqual(0, repo.SaveCount);
    }

    [TestMethod]
    public async Task InactiveTarget_IsRejected()
    {
        var repo = new Fake(ComplaintStatus.InDiagnosis);
        var ex = await Assert.ThrowsExactlyAsync<WorkflowException>(() => new TicketAssignmentService(repo).MoveAsync(101, 20, new(22, 20, "Workload"), Ct));
        Assert.AreEqual(WorkflowError.Invalid, ex.Error);
        Assert.AreEqual<int?>(20, repo.Ticket.AssignedTechnicianId);
        Assert.AreEqual(0, repo.SaveCount);
    }

    [TestMethod]
    public async Task CompletedTicket_CannotBeReassigned()
    {
        var repo = new Fake(ComplaintStatus.Completed);
        var ex = await Assert.ThrowsExactlyAsync<WorkflowException>(() => new TicketAssignmentService(repo).MoveAsync(101, 30, new(21, 20, "Workload"), Ct));
        Assert.AreEqual(WorkflowError.Conflict, ex.Error);
        Assert.AreEqual(0, repo.SaveCount);
    }

    [TestMethod]
    public async Task StaleAdminSelection_CannotOverwriteNewAssignment()
    {
        var repo = new Fake(ComplaintStatus.Approved);
        var ex = await Assert.ThrowsExactlyAsync<WorkflowException>(() => new TicketAssignmentService(repo).MoveAsync(101, 30, new(21, 99, "Workload"), Ct));
        Assert.AreEqual(WorkflowError.Conflict, ex.Error);
        Assert.AreEqual(0, repo.SaveCount);
    }

    [TestMethod]
    public async Task MissingReason_IsRejected()
    {
        var repo = new Fake(ComplaintStatus.InDiagnosis);
        var ex = await Assert.ThrowsExactlyAsync<WorkflowException>(() => new TicketAssignmentService(repo).MoveAsync(101, 20, new(21, 20, " "), Ct));
        Assert.AreEqual(WorkflowError.Invalid, ex.Error);
        Assert.AreEqual(0, repo.SaveCount);
    }

    [TestMethod]
    public async Task InactiveActor_CannotClaim()
    {
        var repo = new Fake();
        var ex = await Assert.ThrowsExactlyAsync<WorkflowException>(() => new TicketAssignmentService(repo).ClaimAsync(101, 22, Ct));
        Assert.AreEqual(WorkflowError.Forbidden, ex.Error);
        Assert.AreEqual(0, repo.SaveCount);
    }

    private sealed class Fake : ITicketAssignmentRepository
    {
        public Complaint Ticket { get; }
        public ComplaintStatusHistory? History { get; private set; }
        public BusinessActivityEvent? Activity { get; private set; }
        public int SaveCount { get; private set; }
        public Fake(ComplaintStatus status = ComplaintStatus.Submitted)
        {
            Ticket = new Complaint { ComplaintId = 101, CustomerId = 1, TicketNumber = "TKT-TEST",
                Status = status, AssignedTechnicianId = status == ComplaintStatus.Submitted ? null : 20 };
        }
        public Task<User?> GetUserAsync(int id, CancellationToken ct) => Task.FromResult<User?>(id switch
        {
            20 or 21 or 22 => new User { UserId = id, FullName = $"Tech {id}", Role = UserRole.Technician, IsActive = id != 22 },
            30 => new User { UserId = id, FullName = "Admin", Role = UserRole.Admin, IsActive = true },
            _ => null
        });
        public Task<Complaint?> GetComplaintAsync(int id, CancellationToken ct) => Task.FromResult<Complaint?>(id == 101 ? Ticket : null);
        public Task<IReadOnlyList<AssignmentTechnicianDto>> GetTechniciansAsync(CancellationToken ct) => Task.FromResult<IReadOnlyList<AssignmentTechnicianDto>>([]);
        public Task<IReadOnlyList<AssignmentTicketDto>> GetBoardAsync(int? id, CancellationToken ct) => Task.FromResult<IReadOnlyList<AssignmentTicketDto>>([]);
        public Task SaveAsync(ComplaintStatusHistory? history, BusinessActivityEvent activity, CancellationToken ct)
        {
            History = history; Activity = activity; SaveCount++;
            return Task.CompletedTask;
        }
    }
}
