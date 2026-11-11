
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairSystem.Application.Contracts.SystemLogs;
using RepairSystem.Application.Interfaces.SystemLogs;

namespace RepairSystem.Api.Controllers;

[ApiController]
[Route("api/admin/system-logs")]
[Authorize(Roles = "Admin")]
public sealed class SystemLogsController : ControllerBase
{
    private readonly ISystemLogService _systemLogService;

    public SystemLogsController(ISystemLogService systemLogService)
    {
        _systemLogService = systemLogService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<SystemLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResultDto<SystemLogDto>>> Search(
        [FromQuery] SystemLogQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _systemLogService.SearchAsync(
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
