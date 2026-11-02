using RepairSystem.Application.Contracts.Complaints;
using RepairSystem.Application.Interfaces.Complaints;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Application.Services.Complaints;

public sealed class ComplaintService : IComplaintService
{
    private readonly IComplaintRepository _complaintRepository;

    public ComplaintService(
        IComplaintRepository complaintRepository)
    {
        _complaintRepository = complaintRepository;
    }

    public async Task<IReadOnlyList<CustomerComplaintDto>>
        GetCustomerComplaintsAsync(int customerId)
    {
        var complaints =
            await _complaintRepository.GetByCustomerIdAsync(customerId);

        return complaints.Select(MapToDto).ToList();
    }

    public async Task<CustomerComplaintDto> CreateCustomerComplaintAsync(
        int customerId,
        CreateComplaintRequestDto request)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTime.UtcNow;
        var ticketSuffix = Guid.NewGuid()
            .ToString("N")[..8]
            .ToUpperInvariant();

        var complaint = new Complaint
        {
            TicketNumber = $"TKT-{now:yyyyMMdd}-{ticketSuffix}",

            CustomerId = customerId,
            AssignedTechnicianId = null,

            DeviceType = request.DeviceType.Trim(),
            DeviceBrand = request.DeviceBrand.Trim(),
            DeviceModel = request.DeviceModel.Trim(),

            SerialNumber = string.IsNullOrWhiteSpace(request.SerialNumber)
                ? null
                : request.SerialNumber.Trim(),

            IssueDescription = request.IssueDescription.Trim(),

            ClaimedWarranty = request.ClaimedWarranty,

            Status = ComplaintStatus.Submitted,

            CreatedAt = now,
            UpdatedAt = now
        };

        await _complaintRepository.AddAsync(complaint);

        return MapToDto(complaint);
    }

    private static CustomerComplaintDto MapToDto(Complaint complaint)
    {
        return new CustomerComplaintDto
        {
            ComplaintId = complaint.ComplaintId,
            TicketNumber = complaint.TicketNumber,
            DeviceType = complaint.DeviceType,
            DeviceBrand = complaint.DeviceBrand,
            DeviceModel = complaint.DeviceModel,
            IssueDescription = complaint.IssueDescription,
            Status = complaint.Status.ToString(),
            CreatedAt = complaint.CreatedAt,
            UpdatedAt = complaint.UpdatedAt
        };
    }
}