
namespace RepairSystem.Application.Contracts.SystemLogs;

public sealed class SystemLogDto
{
    public int Id { get; set; }

    public DateTime? TimeStamp { get; set; }

    public string? Level { get; set; }

    public string? Message { get; set; }

    public string? Exception { get; set; }

    public int? ActorUserId { get; set; }

    public string? ActorRole { get; set; }

    public string? RequestMethod { get; set; }

    public string? RequestPath { get; set; }

    public int? StatusCode { get; set; }

    public double? Elapsed { get; set; }

    public string? CorrelationId { get; set; }

    public string? Action { get; set; }

    public string? ResourceType { get; set; }

    public int? ResourceId { get; set; }

    public string? Outcome { get; set; }

    public string? Application { get; set; }

    public string? Properties { get; set; }
}
