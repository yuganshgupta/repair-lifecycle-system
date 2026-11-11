using RepairSystem.Application.Contracts.Complaints;
using RepairSystem.Application.Interfaces.Complaints;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;
using RepairSystem.Application.Interfaces.Users;

namespace RepairSystem.Application.Services.Complaints;

public sealed class ComplaintService : IComplaintService
{
    private readonly IComplaintRepository _complaintRepository;
    private readonly IUserRepository _userRepository;

    public ComplaintService(
    IComplaintRepository complaintRepository,
    IUserRepository userRepository)
    {
        _complaintRepository = complaintRepository;
        _userRepository = userRepository;
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

    public async Task<IReadOnlyList<TechnicianComplaintDto>>
    GetTechnicianComplaintsAsync(int technicianId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(technicianId);

        var complaints =
            await _complaintRepository.GetAssignedToTechnicianAsync(
                technicianId);

        return complaints.Select(complaint =>
            new TechnicianComplaintDto
            {
                ComplaintId = complaint.ComplaintId,
                TicketNumber = complaint.TicketNumber,

                CustomerName = complaint.Customer.FullName,

                DeviceType = complaint.DeviceType,
                DeviceBrand = complaint.DeviceBrand,
                DeviceModel = complaint.DeviceModel,
                SerialNumber = complaint.SerialNumber,

                IssueDescription = complaint.IssueDescription,
                ClaimedWarranty = complaint.ClaimedWarranty,

                Status = complaint.Status.ToString(),

                CreatedAt = complaint.CreatedAt,
                UpdatedAt = complaint.UpdatedAt
            })
            .ToList();
    }

    public async Task<ComplaintAssignmentResult> AssignTechnicianAsync(
    int complaintId,
    int technicianId,
    int assignedByAdminId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(complaintId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(technicianId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assignedByAdminId);

        // 1. Find the complaint.
        var complaint = await _complaintRepository.GetByIdAsync(complaintId);

        if (complaint is null)
        {
            return ComplaintAssignmentResult.ComplaintNotFound;
        }

        // 2. Find the target technician.
        var technician = await _userRepository.GetByIdAsync(technicianId);

        if (technician is null)
        {
            return ComplaintAssignmentResult.TechnicianNotFound;
        }

        // 3. Verify the user's role.
        if (technician.Role != UserRole.Technician)
        {
            return ComplaintAssignmentResult.UserIsNotTechnician;
        }

        // 4. Verify that the technician account is active.
        if (!technician.IsActive)
        {
            return ComplaintAssignmentResult.TechnicianInactive;
        }

        // 5. Verify that the complaint is not already assigned.
        if (complaint.AssignedTechnicianId.HasValue)
        {
            return ComplaintAssignmentResult.AlreadyAssigned;
        }

        // 6. Only Submitted complaints can be assigned.
        if (complaint.Status != ComplaintStatus.Submitted)
        {
            return ComplaintAssignmentResult.InvalidComplaintStatus;
        }

        var previousStatus = complaint.Status;
        var changedAt = DateTime.UtcNow;

        // 7. Apply the domain assignment operation.
        complaint.AssignToTechnician(technicianId);

        // 8. Create the audit-history record.
        var history = new ComplaintStatusHistory
        {
            ComplaintId = complaint.ComplaintId,
            PreviousStatus = previousStatus,
            NewStatus = complaint.Status,
            ChangedByUserId = assignedByAdminId,
            ChangedAt = changedAt
        };

        // 9. Persist the assignment and history together.
        await _complaintRepository.SaveAssignmentAsync(
            complaint,
            history);

        return ComplaintAssignmentResult.Assigned;
    }

    public async Task<IReadOnlyList<AdminUnassignedComplaintDto>>
    GetUnassignedComplaintsAsync()
    {
        var complaints =
            await _complaintRepository.GetUnassignedAsync();

        return complaints.Select(complaint =>
            new AdminUnassignedComplaintDto
            {
                ComplaintId = complaint.ComplaintId,
                TicketNumber = complaint.TicketNumber,
                CustomerName = complaint.Customer.FullName,
                DeviceType = complaint.DeviceType,
                DeviceBrand = complaint.DeviceBrand,
                DeviceModel = complaint.DeviceModel,
                IssueDescription = complaint.IssueDescription,
                ClaimedWarranty = complaint.ClaimedWarranty,
                Status = complaint.Status.ToString(),
                CreatedAt = complaint.CreatedAt
            })
            .ToList();
    }
}