namespace RepairSystem.Web.Features.Admin.Contracts;

public sealed class BusinessActivityApiDto
{
    public int BusinessActivityEventId { get; init; }
    public int EventTypeId { get; init; }
    public string EventType { get; init; } = "";

    public DateTime OccurredAtUtc { get; init; }

    public int? ActorUserId { get; init; }
    public string ActorName { get; init; } = "";
    public string ActorRole { get; init; } = "";

    public int? TargetUserId { get; init; }
    public string? TargetUserName { get; init; }

    public int? CustomerId { get; init; }
    public string? CustomerName { get; init; }

    public int? ComplaintId { get; init; }
    public string? TicketNumber { get; init; }

    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public string? DetailsJson { get; init; }
}

public sealed class PagedBusinessActivityApiResponse
{
    public List<BusinessActivityApiDto> Items { get; init; } = [];

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}