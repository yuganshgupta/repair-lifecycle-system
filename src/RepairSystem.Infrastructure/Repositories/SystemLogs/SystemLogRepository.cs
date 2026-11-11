
using Microsoft.EntityFrameworkCore;
using RepairSystem.Application.Contracts.SystemLogs;
using RepairSystem.Application.Interfaces.SystemLogs;
using RepairSystem.Infrastructure.Data;
using RepairSystem.Infrastructure.ReadModels;

namespace RepairSystem.Infrastructure.Repositories.SystemLogs;

public sealed class SystemLogRepository : ISystemLogRepository
{
    private readonly RepairDbContext _dbContext;

    public SystemLogRepository(RepairDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResultDto<SystemLogDto>> SearchAsync(
        SystemLogQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Start with a read-only query against the existing Serilog table.
        IQueryable<SystemLogEntry> logs =
            _dbContext.SystemLogs.AsNoTracking();

        // 1. Global search.
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.Trim();

            logs = logs.Where(log =>
                (log.Message != null &&
                 log.Message.Contains(searchTerm)) ||

                (log.RequestPath != null &&
                 log.RequestPath.Contains(searchTerm)) ||

                (log.Action != null &&
                 log.Action.Contains(searchTerm)) ||

                (log.ResourceType != null &&
                 log.ResourceType.Contains(searchTerm)) ||

                (log.CorrelationId != null &&
                 log.CorrelationId.Contains(searchTerm)) ||

                (log.Application != null &&
                 log.Application.Contains(searchTerm)));
        }

        // 2. Log level filter.
        if (!string.IsNullOrWhiteSpace(query.Level))
        {
            var level = query.Level.Trim();

            logs = logs.Where(log => log.Level == level);
        }

        // 3. HTTP method filter.
        if (!string.IsNullOrWhiteSpace(query.RequestMethod))
        {
            var method = query.RequestMethod
                .Trim()
                .ToUpperInvariant();

            logs = logs.Where(log => log.RequestMethod == method);
        }

        // 4. HTTP status filter.
        if (query.StatusCode.HasValue)
        {
            var statusCode = query.StatusCode.Value;

            logs = logs.Where(log =>
                log.StatusCode == statusCode);
        }

        // 5. Actor user filter.
        if (query.ActorUserId.HasValue)
        {
            var actorUserId = query.ActorUserId.Value;

            logs = logs.Where(log =>
                log.ActorUserId == actorUserId);
        }

        // 6. Actor role filter.
        if (!string.IsNullOrWhiteSpace(query.ActorRole))
        {
            var actorRole = query.ActorRole.Trim();

            logs = logs.Where(log =>
                log.ActorRole == actorRole);
        }

        // 7. Date range filters.
        if (query.FromUtc.HasValue)
        {
            var fromUtc = query.FromUtc.Value;

            logs = logs.Where(log =>
                log.TimeStamp >= fromUtc);
        }

        if (query.ToUtc.HasValue)
        {
            var toUtc = query.ToUtc.Value;

            logs = logs.Where(log =>
                log.TimeStamp <= toUtc);
        }

        // 8. Count matching records before pagination.
        var totalCount = await logs.CountAsync(cancellationToken);

        // Defensively constrain pagination.
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        // Avoid integer overflow when calculating the offset.
        var skip = (int)Math.Min(
            (long)(page - 1) * pageSize,
            int.MaxValue);

        // 9. Use an explicit sorting allowlist.
        var sortBy = query.SortBy?.Trim().ToLowerInvariant();

        IOrderedQueryable<SystemLogEntry> orderedLogs =
            (sortBy, query.SortDescending) switch
            {
                ("level", false) =>
                    logs.OrderBy(log => log.Level),

                ("level", true) =>
                    logs.OrderByDescending(log => log.Level),

                ("method", false) =>
                    logs.OrderBy(log => log.RequestMethod),

                ("method", true) =>
                    logs.OrderByDescending(log => log.RequestMethod),

                ("statuscode", false) =>
                    logs.OrderBy(log => log.StatusCode),

                ("statuscode", true) =>
                    logs.OrderByDescending(log => log.StatusCode),

                ("elapsed", false) =>
                    logs.OrderBy(log => log.Elapsed),

                ("elapsed", true) =>
                    logs.OrderByDescending(log => log.Elapsed),

                ("actoruserid", false) =>
                    logs.OrderBy(log => log.ActorUserId),

                ("actoruserid", true) =>
                    logs.OrderByDescending(log => log.ActorUserId),

                ("actorrole", false) =>
                    logs.OrderBy(log => log.ActorRole),

                ("actorrole", true) =>
                    logs.OrderByDescending(log => log.ActorRole),

                ("endpoint", false) =>
                    logs.OrderBy(log => log.RequestPath),

                ("endpoint", true) =>
                    logs.OrderByDescending(log => log.RequestPath),

                ("application", false) =>
                    logs.OrderBy(log => log.Application),

                ("application", true) =>
                    logs.OrderByDescending(log => log.Application),

                ("timestamp", false) =>
                    logs.OrderBy(log => log.TimeStamp),

                _ =>
                    logs.OrderByDescending(log => log.TimeStamp)
            };

        // Use Id as a stable secondary sort.
        orderedLogs = orderedLogs.ThenByDescending(log => log.Id);

        // 10. Retrieve only the requested page.
        var items = await orderedLogs
            .Skip(skip)
            .Take(pageSize)
            .Select(log => new SystemLogDto
            {
                Id = log.Id,
                TimeStamp = log.TimeStamp,
                Level = log.Level,
                Message = log.Message,
                Exception = log.Exception,

                ActorUserId = log.ActorUserId,
                ActorRole = log.ActorRole,

                RequestMethod = log.RequestMethod,
                RequestPath = log.RequestPath,
                StatusCode = log.StatusCode,
                Elapsed = log.Elapsed,

                CorrelationId = log.CorrelationId,

                Action = log.Action,
                ResourceType = log.ResourceType,
                ResourceId = log.ResourceId,
                Outcome = log.Outcome,

                Application = log.Application,
                Properties = log.Properties
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<SystemLogDto>(
            items,
            page,
            pageSize,
            totalCount);
    }
}
