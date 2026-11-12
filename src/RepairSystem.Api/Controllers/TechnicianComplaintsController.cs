
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

    [HttpPatch("{complaintId:int}/status")]
    public async Task<IActionResult> UpdateComplaintStatus(
        int complaintId,
        [FromBody] UpdateTechnicianComplaintStatusRequestDto request)
    {
        var userIdValue = User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdValue, out var technicianId) ||
            technicianId <= 0)
        {
            return Unauthorized();
        }

        var result =
            await _complaintService.UpdateTechnicianComplaintStatusAsync(
                complaintId,
                technicianId,
                request.NewStatus);

        return result switch
        {
            TechnicianStatusUpdateResult.Updated =>
                NoContent(),

            // Avoid revealing whether another Technician owns this ID.
            TechnicianStatusUpdateResult.ComplaintNotFound or
            TechnicianStatusUpdateResult.ComplaintNotAssignedToTechnician =>
                NotFound(),

            TechnicianStatusUpdateResult.InvalidStatus =>
                BadRequest(new
                {
                    message = "The requested complaint status is invalid."
                }),

            TechnicianStatusUpdateResult.InvalidTransition =>
                Conflict(new
                {
                    message = "This complaint cannot be moved to the requested status."
                }),

            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
