using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairSystem.Application.Contracts.Complaints;
using RepairSystem.Application.Interfaces.Complaints;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Api.Controllers;

[ApiController]
[Route("api/technician/complaints")]
[Authorize(Roles = nameof(UserRole.Technician))]
public sealed class TechnicianComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaintService;

    public TechnicianComplaintsController(
        IComplaintService complaintService)
    {
        _complaintService = complaintService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TechnicianComplaintDto>>>
        GetAssignedComplaints()
    {
        var userIdValue = User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdValue, out var technicianId) ||
            technicianId <= 0)
        {
            return Unauthorized();
        }

        var complaints =
            await _complaintService.GetTechnicianComplaintsAsync(
                technicianId);

        return Ok(complaints);
    }
}