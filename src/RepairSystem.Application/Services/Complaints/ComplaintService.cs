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

        var customer = await _userRepository.GetByIdAsync(customerId)
    ?? throw new InvalidOperationException(
        "The customer account was not found.");


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

        var initialHistory = new ComplaintStatusHistory
        {
            Complaint = complaint,
            PreviousStatus = null,
            NewStatus = ComplaintStatus.Submitted,
            ChangedByUserId = customerId,
            ChangedAt = now
        };

        await _complaintRepository.AddWithHistoryAndActivityAsync(
    complaint,
    initialHistory,
    savedComplaint => BusinessActivityEvent.Create(
        eventType: BusinessActivityType.ComplaintSubmitted,
        actorName: customer.FullName,
        actorRole: customer.Role.ToString(),
        title: "Complaint submitted",
        description:
            $"Customer submitted complaint {savedComplaint.TicketNumber}.",
        actorUserId: customerId,
        customerId: customerId,
        customerName: customer.FullName,
        complaintId: savedComplaint.ComplaintId,
        ticketNumber: savedComplaint.TicketNumber
    )
);

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

        // Retrieve the administrator performing the assignment.
        var admin = await _userRepository.GetByIdAsync(
            assignedByAdminId)
            ?? throw new InvalidOperationException(
                "The administrator account was not found.");

        // Retrieve the customer who owns the complaint.
        var customer = await _userRepository.GetByIdAsync(
            complaint.CustomerId)
            ?? throw new InvalidOperationException(
                "The complaint customer account was not found.");

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

        // Create an activity describing the successful assignment.
        var activity = BusinessActivityEvent.Create(
            eventType: BusinessActivityType.TechnicianAssigned,
            actorName: admin.FullName,
            actorRole: admin.Role.ToString(),
            title: "Technician assigned",
            description:
                $"Administrator {admin.FullName} assigned " +
                $"{technician.FullName} to complaint " +
                $"{complaint.TicketNumber}.",
            actorUserId: assignedByAdminId,
            targetUserId: technicianId,
            targetUserName: technician.FullName,
            customerId: complaint.CustomerId,
            customerName: customer.FullName,
            complaintId: complaint.ComplaintId,
            ticketNumber: complaint.TicketNumber
        );

        // Save the assignment, status history and business event.
        await _complaintRepository.SaveAssignmentAsync(
            complaint,
            history,
            activity);

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


    public async Task<TechnicianStatusUpdateResult>
        UpdateTechnicianComplaintStatusAsync(
            int complaintId,
            int technicianId,
            ComplaintStatus newStatus)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(complaintId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(technicianId);

        if (!Enum.IsDefined(newStatus))
        {
            return TechnicianStatusUpdateResult.InvalidStatus;
        }

        var complaint =
            await _complaintRepository.GetByIdAsync(complaintId);

        if (complaint is null)
        {
            return TechnicianStatusUpdateResult.ComplaintNotFound;
        }

        // A Technician can only update their own assigned complaints.
        if (complaint.AssignedTechnicianId != technicianId)
        {
            return TechnicianStatusUpdateResult
                .ComplaintNotAssignedToTechnician;
        }

        // Technician-specific permissions.
        // Quote approval and cancellation are not Technician actions.
        var allowedForTechnician =
            (complaint.Status, newStatus) switch
            {
                (ComplaintStatus.Assigned, ComplaintStatus.InDiagnosis)
                    => true,

                (ComplaintStatus.Approved, ComplaintStatus.InRepair)
                    => true,

                (ComplaintStatus.InRepair, ComplaintStatus.Completed)
                    => true,

                _ => false
            };

        if (!allowedForTechnician)
        {
            return TechnicianStatusUpdateResult.InvalidTransition;
        }

        var previousStatus = complaint.Status;

        try
        {
            complaint.ChangeStatus(newStatus);
        }
        catch (InvalidOperationException)
        {
            return TechnicianStatusUpdateResult.InvalidTransition;
        }

        var history = new ComplaintStatusHistory
        {
            ComplaintId = complaint.ComplaintId,
            PreviousStatus = previousStatus,
            NewStatus = complaint.Status,
            ChangedByUserId = technicianId,
            ChangedAt = complaint.UpdatedAt
        };

        var technician = await _userRepository.GetByIdAsync(
    technicianId)
    ?? throw new InvalidOperationException(
        "The technician account was not found.");

        var (eventType, title) = newStatus switch
        {
            ComplaintStatus.InDiagnosis => (
                BusinessActivityType.DiagnosisStarted,
                "Diagnosis started"),

            ComplaintStatus.InRepair => (
                BusinessActivityType.RepairStarted,
                "Repair started"),

            ComplaintStatus.Completed => (
                BusinessActivityType.RepairCompleted,
                "Repair completed"),

            _ => throw new InvalidOperationException(
                "Unsupported technician activity.")
        };

        var activity = BusinessActivityEvent.Create(
            eventType: eventType,
            actorName: technician.FullName,
            actorRole: technician.Role.ToString(),
            title: title,
            description:
                $"Technician {technician.FullName} updated complaint " +
                $"{complaint.TicketNumber} from {previousStatus} to {newStatus}.",
            actorUserId: technicianId,
            customerId: complaint.CustomerId,
            complaintId: complaint.ComplaintId,
            ticketNumber: complaint.TicketNumber
        );

        await _complaintRepository.SaveStatusChangeAsync(
            complaint,
            history,
            activity);

        return TechnicianStatusUpdateResult.Updated;
    }

    public async Task<IReadOnlyList<CustomerComplaintHistoryDto>?>
    GetCustomerComplaintHistoryAsync(
        int customerId,
        int complaintId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(complaintId);

        // First verify that the complaint belongs to this Customer.
        var complaint = await _complaintRepository.GetByIdAsync(
            complaintId);

        if (complaint is null || complaint.CustomerId != customerId)
            return null;

        // Retrieve history only after ownership is established.
        var history =
            await _complaintRepository.GetStatusHistoryByComplaintIdAsync(
                complaintId);

        return history.Select(item => new CustomerComplaintHistoryDto
        {
            HistoryId = item.ComplaintStatusHistoryId,

            PreviousStatus = item.PreviousStatus?.ToString(),

            NewStatus = item.NewStatus.ToString(),

            ChangedByName = item.ChangedByUser.FullName,

            ChangedByRole = item.ChangedByUser.Role.ToString(),

            ChangedAt = item.ChangedAt
        }).ToList();
    }

}