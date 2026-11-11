
using System.Text.Json.Serialization;

namespace RepairSystem.Web.Features.Admin.Contracts;

public sealed class PagedSystemLogApiResponse
{
    [JsonPropertyName("items")]
    public List<SystemLogApiDto> Items { get; init; } = [];

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; init; }
}
