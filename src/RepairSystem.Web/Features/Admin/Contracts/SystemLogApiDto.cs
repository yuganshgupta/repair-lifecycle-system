
using System.Text.Json.Serialization;

namespace RepairSystem.Web.Features.Admin.Contracts;

public sealed class SystemLogApiDto
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("timeStamp")]
    public DateTime? TimeStamp { get; init; }

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("exception")]
    public string? Exception { get; init; }

    [JsonPropertyName("actorUserId")]
    public int? ActorUserId { get; init; }

    [JsonPropertyName("actorRole")]
    public string? ActorRole { get; init; }

    [JsonPropertyName("requestMethod")]
    public string? RequestMethod { get; init; }

    [JsonPropertyName("requestPath")]
    public string? RequestPath { get; init; }

    [JsonPropertyName("statusCode")]
    public int? StatusCode { get; init; }

    [JsonPropertyName("elapsed")]
    public double? Elapsed { get; init; }

    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }

    [JsonPropertyName("action")]
    public string? Action { get; init; }

    [JsonPropertyName("resourceType")]
    public string? ResourceType { get; init; }

    [JsonPropertyName("resourceId")]
    public int? ResourceId { get; init; }

    [JsonPropertyName("outcome")]
    public string? Outcome { get; init; }

    [JsonPropertyName("application")]
    public string? Application { get; init; }

    [JsonPropertyName("properties")]
    public string? Properties { get; init; }
}
