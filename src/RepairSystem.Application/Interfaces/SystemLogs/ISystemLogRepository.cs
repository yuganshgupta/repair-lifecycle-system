
using RepairSystem.Application.Contracts.SystemLogs;

namespace RepairSystem.Application.Interfaces.SystemLogs;

public interface ISystemLogRepository
{
    Task<PagedResultDto<SystemLogDto>> SearchAsync(
        SystemLogQuery query,
        CancellationToken cancellationToken = default);
}
