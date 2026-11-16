using System.Text.Json.Serialization;

namespace RepairSystem.Web.Features.Dashboard.Contracts;

public sealed class CustomerComplaintHistoryApiDto
{
    [JsonPropertyName("historyId")]
    public int HistoryId { get; init; }

    [JsonPropertyName("previousStatus")]
    public string? PreviousStatus { get; init; }

    [JsonPropertyName("newStatus")]
    public string NewStatus { get; init; } = string.Empty;

    [JsonPropertyName("changedByName")]
    public string ChangedByName { get; init; } = string.Empty;

    [JsonPropertyName("changedByRole")]
    public string ChangedByRole { get; init; } = string.Empty;

    [JsonPropertyName("changedAt")]
    public DateTime ChangedAt { get; init; }
}