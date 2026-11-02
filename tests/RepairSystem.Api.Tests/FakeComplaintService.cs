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

    public IReadOnlyList<CustomerComplaintDto> Complaints { get; set; }
        = Array.Empty<CustomerComplaintDto>();

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

    public Task<IReadOnlyList<CustomerComplaintDto>>
        GetCustomerComplaintsAsync(int customerId)
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
}