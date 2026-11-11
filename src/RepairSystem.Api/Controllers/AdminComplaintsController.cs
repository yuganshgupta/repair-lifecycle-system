
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairSystem.Application.Contracts.Complaints;
using RepairSystem.Application.Interfaces.Complaints;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Api.Controllers;

[ApiController]
[Route("api/admin/complaints")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AdminComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaintService;

    public AdminComplaintsController(
        IComplaintService complaintService)
    {
        _complaintService = complaintService;
    }

    [HttpPost("{complaintId:int}/assign")]
    public async Task<IActionResult> AssignTechnician(
        int complaintId,
        [FromBody] AssignTechnicianRequestDto request)
    {
        if (complaintId <= 0)
        {
            return BadRequest(new
            {
                message = "A valid complaint ID is required."
            });
        }

        // The Admin ID comes from the authenticated JWT,
        // never from the request body.
        var adminIdValue = User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(adminIdValue, out var adminId) ||
            adminId <= 0)
        {
            return Unauthorized();
        }

        var result = await _complaintService.AssignTechnicianAsync(
            complaintId,
            request.TechnicianId,
            adminId);

        return result switch
        {
            ComplaintAssignmentResult.Assigned =>
                NoContent(),

            ComplaintAssignmentResult.ComplaintNotFound =>
                NotFound(new
                {
                    message = "The complaint could not be found."
                }),

            ComplaintAssignmentResult.TechnicianNotFound =>
                NotFound(new
                {
                    message = "The selected Technician could not be found."
                }),

            ComplaintAssignmentResult.TechnicianInactive =>
                Conflict(new
                {
                    message = "The selected Technician account is inactive."
                }),

            ComplaintAssignmentResult.UserIsNotTechnician =>
                BadRequest(new
                {
                    message = "The selected user is not a Technician."
                }),

            ComplaintAssignmentResult.AlreadyAssigned =>
                Conflict(new
                {
                    message = "This complaint is already assigned."
                }),

            ComplaintAssignmentResult.InvalidComplaintStatus =>
                Conflict(new
                {
                    message = "Only submitted complaints can be assigned."
                }),

            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [HttpGet("unassigned")]
    public async Task<ActionResult<IReadOnlyList<AdminUnassignedComplaintDto>>>
    GetUnassignedComplaints()
    {
        var complaints =
            await _complaintService.GetUnassignedComplaintsAsync();

        return Ok(complaints);
    }
}
