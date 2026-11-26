using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairSystem.Application.Contracts.Assignments;
using RepairSystem.Application.Contracts.RepairWorkflow;
using RepairSystem.Application.Services.Assignments;

namespace RepairSystem.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Technician")]
[Route("api/ticket-assignments")]
public sealed class TicketAssignmentsController(TicketAssignmentService service) : ControllerBase
{
    [HttpGet]
    public Task<IActionResult> Get(CancellationToken ct) => ExecuteAsync(async id => Ok(await service.GetBoardAsync(id, ct)));

    [HttpPost("{complaintId:int:min(1)}/claim")]
    [Authorize(Roles = "Technician")]
    public Task<IActionResult> Claim(int complaintId, CancellationToken ct) => ExecuteAsync(async id =>
    {
        await service.ClaimAsync(complaintId, id, ct);
        return NoContent();
    });

    [HttpPost("{complaintId:int:min(1)}/move")]
    public Task<IActionResult> Move(int complaintId, MoveAssignmentRequest request, CancellationToken ct) => ExecuteAsync(async id =>
    {
        await service.MoveAsync(complaintId, id, request, ct);
        return NoContent();
    });

    private async Task<IActionResult> ExecuteAsync(Func<int, Task<IActionResult>> action)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) || id <= 0)
            return Unauthorized();
        try { return await action(id); }
        catch (WorkflowException ex)
        {
            return ex.Error switch
            {
                WorkflowError.Invalid => BadRequest(new { message = ex.Message }),
                WorkflowError.Forbidden => StatusCode(403, new { message = ex.Message }),
                WorkflowError.NotFound => NotFound(new { message = ex.Message }),
                WorkflowError.Conflict => Conflict(new { message = ex.Message }),
                _ => StatusCode(500)
            };
        }
    }
}
