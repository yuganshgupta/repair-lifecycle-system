using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairSystem.Application.Contracts.BusinessActivity;
using RepairSystem.Application.Interfaces.BusinessActivity;
using RepairSystem.Application.Services.BusinessActivity;

namespace RepairSystem.Api.Controllers;

[ApiController]
[Route("api/admin/business-activity")]
[Authorize(Roles = "Admin")]
public sealed class BusinessActivityController : ControllerBase
{
    private readonly IBusinessActivityService _service;

    public BusinessActivityController(
        IBusinessActivityService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedBusinessActivityDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedBusinessActivityDto>> Search(
        [FromQuery] BusinessActivityQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.SearchAsync(
                query,
                cancellationToken);

            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new
            {
                message = exception.Message
            });
        }
    }
}