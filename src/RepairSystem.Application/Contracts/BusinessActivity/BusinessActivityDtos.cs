namespace RepairSystem.Application.Contracts.BusinessActivity;

// Filters and pagination parameters accepted by the API.
public sealed class BusinessActivityQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    public int? EventType { get; set; }
    public int? ActorUserId { get; set; }
    public int? ComplaintId { get; set; }

    public string? SearchTerm { get; set; }

    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtcExclusive { get; set; }
}

// One activity record returned to the administrator.
public sealed class BusinessActivityDto
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

// Standard paginated response for the Blazor table.
public sealed class PagedBusinessActivityDto
{
    public IReadOnlyList<BusinessActivityDto> Items { get; init; }
        = [];

    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}