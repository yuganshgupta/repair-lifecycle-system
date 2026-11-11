
namespace RepairSystem.Infrastructure.ReadModels;

/// <summary>
/// Read-only representation of a Serilog event stored in SQL Server.
/// The table itself is managed by Serilog, not EF Core.
/// </summary>
public sealed class SystemLogEntry
{
    public int Id { get; set; }

    public string? Message { get; set; }
    public string? MessageTemplate { get; set; }
    public string? Level { get; set; }
    public DateTime? TimeStamp { get; set; }
    public string? Exception { get; set; }
    public string? Properties { get; set; }

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
}
