
namespace RepairSystem.Application.Contracts.SystemLogs;

public sealed class SystemLogQuery
{
    public string? SearchTerm { get; set; }

    public string? Level { get; set; }

    public string? RequestMethod { get; set; }

    public int? StatusCode { get; set; }

    public int? ActorUserId { get; set; }

    public string? ActorRole { get; set; }

    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;

    public string SortBy { get; set; } = "timestamp";

    public bool SortDescending { get; set; } = true;
}
