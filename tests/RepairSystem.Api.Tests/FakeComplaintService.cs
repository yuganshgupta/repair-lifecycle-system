using RepairSystem.Application.Contracts.Complaints;
using RepairSystem.Application.Interfaces.Complaints;

namespace RepairSystem.Api.Tests;

public sealed class FakeComplaintService : IComplaintService
{
    public int CallCount { get; private set; }

    public int CreateCallCount { get; private set; }

    public int? LastRequestedCustomerId { get; private set; }

    public int? LastCreatedCustomerId { get; private set; }

    public CreateComplaintRequestDto? LastCreateRequest { get; private set; }

    public int TechnicianCallCount { get; private set; }

    public int? LastRequestedTechnicianId { get; private set; }

    public IReadOnlyList<TechnicianComplaintDto> TechnicianComplaints { get; set; }
        = Array.Empty<TechnicianComplaintDto>();

    public IReadOnlyList<CustomerComplaintDto> Complaints { get; set; }
        = Array.Empty<CustomerComplaintDto>();

    public int AssignmentCallCount { get; private set; }

    public int? LastAssignedComplaintId { get; private set; }

    public int? LastAssignedTechnicianId { get; private set; }

    public int? LastAssigningAdminId { get; private set; }

    public ComplaintAssignmentResult AssignmentResult { get; set; }
        = ComplaintAssignmentResult.Assigned;

    public IReadOnlyList<AdminUnassignedComplaintDto> UnassignedComplaints { get; set; }
        = Array.Empty<AdminUnassignedComplaintDto>();

    public CustomerComplaintDto CreatedComplaint { get; set; } = new()
    {
        ComplaintId = 101,
        TicketNumber = "TKT-20261030-TEST0001",
        DeviceType = "Laptop",
        DeviceBrand = "Dell",
        DeviceModel = "Inspiron 15",
        IssueDescription = "Unexpected shutdown",
        Status = "Submitted",
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public Task<IReadOnlyList<CustomerComplaintDto>> GetCustomerComplaintsAsync(
        int customerId)
    {
        CallCount++;
        LastRequestedCustomerId = customerId;

        return Task.FromResult(Complaints);
    }

    public Task<CustomerComplaintDto> CreateCustomerComplaintAsync(
        int customerId,
        CreateComplaintRequestDto request)
    {
        CreateCallCount++;
        LastCreatedCustomerId = customerId;
        LastCreateRequest = request;

        return Task.FromResult(CreatedComplaint);
    }

    public Task<IReadOnlyList<TechnicianComplaintDto>> GetTechnicianComplaintsAsync(
        int technicianId)
    {
        TechnicianCallCount++;
        LastRequestedTechnicianId = technicianId;

        return Task.FromResult(TechnicianComplaints);
    }

    public Task<ComplaintAssignmentResult> AssignTechnicianAsync(
        int complaintId,
        int technicianId,
        int assignedByAdminId)
    {
        AssignmentCallCount++;

        LastAssignedComplaintId = complaintId;
        LastAssignedTechnicianId = technicianId;
        LastAssigningAdminId = assignedByAdminId;

        return Task.FromResult(AssignmentResult);
    }

    public Task<IReadOnlyList<AdminUnassignedComplaintDto>> GetUnassignedComplaintsAsync()
    {
        return Task.FromResult(UnassignedComplaints);
    }
}