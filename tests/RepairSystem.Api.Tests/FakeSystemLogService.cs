
using RepairSystem.Application.Contracts.SystemLogs;
using RepairSystem.Application.Interfaces.SystemLogs;

namespace RepairSystem.Api.Tests;

public sealed class FakeSystemLogService : ISystemLogService
{
    public int SearchCallCount { get; private set; }

    public SystemLogQuery? LastQuery { get; private set; }

    public Exception? ExceptionToThrow { get; set; }

    public PagedResultDto<SystemLogDto> Result { get; set; } =
        new(
            new[]
            {
                new SystemLogDto
                {
                    Id = 1,
                    TimeStamp = DateTime.UtcNow,
                    Level = "Information",
                    Message = "HTTP GET /api/Auth/users responded 200",
                    ActorUserId = 3,
                    ActorRole = "Admin",
                    RequestMethod = "GET",
                    RequestPath = "/api/Auth/users",
                    StatusCode = 200,
                    Elapsed = 12.5,
                    CorrelationId = "test-correlation-id",
                    Application = "RepairSystem.Api"
                }
            },
            1,
            25,
            1);

    public Task<PagedResultDto<SystemLogDto>> SearchAsync(
        SystemLogQuery query,
        CancellationToken cancellationToken = default)
    {
        SearchCallCount++;
        LastQuery = query;

        if (ExceptionToThrow is not null)
        {
            return Task.FromException<PagedResultDto<SystemLogDto>>(
                ExceptionToThrow);
        }

        return Task.FromResult(Result);
    }
}
