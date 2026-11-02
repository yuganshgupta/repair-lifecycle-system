using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairSystem.Application.Contracts.Complaints;
using RepairSystem.Application.Interfaces.Complaints;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = nameof(UserRole.Customer))]
public sealed class ComplaintsController : ControllerBase
{
    private readonly IComplaintService _complaintService;

    public ComplaintsController(
        IComplaintService complaintService)
    {
        _complaintService = complaintService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerComplaintDto>>>
        GetMyComplaints()
    {
        if (!TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        var complaints =
            await _complaintService.GetCustomerComplaintsAsync(customerId);

        return Ok(complaints);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerComplaintDto>> CreateComplaint(
        [FromBody] CreateComplaintRequestDto request)
    {
        if (!TryGetCustomerId(out var customerId))
        {
            return Unauthorized();
        }

        var complaint =
            await _complaintService.CreateCustomerComplaintAsync(
                customerId,
                request);

        return StatusCode(
            StatusCodes.Status201Created,
            complaint);
    }

    private bool TryGetCustomerId(out int customerId)
    {
        var userIdValue = User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(userIdValue, out customerId) &&
               customerId > 0;
    }
}