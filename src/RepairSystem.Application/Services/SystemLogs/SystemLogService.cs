
using RepairSystem.Application.Contracts.SystemLogs;
using RepairSystem.Application.Interfaces.SystemLogs;

namespace RepairSystem.Application.Services.SystemLogs;

public sealed class SystemLogService : ISystemLogService
{
    private readonly ISystemLogRepository _repository;

    private static readonly HashSet<string> AllowedSortColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "timestamp",
            "level",
            "method",
            "statuscode",
            "elapsed",
            "actoruserid",
            "actorrole",
            "endpoint",
            "application"
        };

    public SystemLogService(ISystemLogRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<SystemLogDto>> SearchAsync(
        SystemLogQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Validate the date range.
        if (query.FromUtc.HasValue &&
            query.ToUtc.HasValue &&
            query.FromUtc.Value > query.ToUtc.Value)
        {
            throw new ArgumentException(
                "The start date cannot be later than the end date.",
                nameof(query));
        }

        // Validate the HTTP status code.
        if (query.StatusCode is < 100 or > 599)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.StatusCode),
                "HTTP status code must be between 100 and 599.");
        }

        // Validate the user ID.
        if (query.ActorUserId is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(query.ActorUserId));
        }

        // Normalize and validate the requested sort column.
        var sortBy = NormalizeOptionalText(
            query.SortBy,
            nameof(query.SortBy),
            32) ?? "timestamp";

        if (!AllowedSortColumns.Contains(sortBy))
        {
            throw new ArgumentException(
                "The requested sorting column is not supported.",
                nameof(query.SortBy));
        }

        // Create a sanitized query rather than mutating the caller's object.
        var normalizedQuery = new SystemLogQuery
        {
            SearchTerm = NormalizeOptionalText(
                query.SearchTerm,
                nameof(query.SearchTerm),
                200),

            Level = NormalizeOptionalText(
                query.Level,
                nameof(query.Level),
                16),

            RequestMethod = NormalizeOptionalText(
                query.RequestMethod,
                nameof(query.RequestMethod),
                16)?.ToUpperInvariant(),

            StatusCode = query.StatusCode,

            ActorUserId = query.ActorUserId,

            ActorRole = NormalizeOptionalText(
                query.ActorRole,
                nameof(query.ActorRole),
                32),

            FromUtc = query.FromUtc,
            ToUtc = query.ToUtc,

            Page = Math.Max(1, query.Page),

            PageSize = Math.Clamp(
                query.PageSize,
                1,
                100),

            SortBy = sortBy,
            SortDescending = query.SortDescending
        };

        return await _repository.SearchAsync(
            normalizedQuery,
            cancellationToken);
    }

    private static string? NormalizeOptionalText(
        string? value,
        string parameterName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"The value cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}
