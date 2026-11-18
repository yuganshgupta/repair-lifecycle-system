using RepairSystem.Application.Contracts.BusinessActivity;
using RepairSystem.Application.Interfaces.BusinessActivity;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Application.Services.BusinessActivity;

public sealed class BusinessActivityService
    : IBusinessActivityService
{
    private readonly IBusinessActivityRepository _repository;

    public BusinessActivityService(
        IBusinessActivityRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedBusinessActivityDto> SearchAsync(
        BusinessActivityQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page < 1 || query.Page > 100_000)
            throw new ArgumentOutOfRangeException(
                nameof(query.Page),
                "Page must be between 1 and 100000.");

        if (query.PageSize < 1 || query.PageSize > 100)
            throw new ArgumentOutOfRangeException(
                nameof(query.PageSize),
                "Page size must be between 1 and 100.");

        if (query.EventType.HasValue &&
            !Enum.IsDefined(
                typeof(BusinessActivityType),
                query.EventType.Value))
        {
            throw new ArgumentException(
                "Invalid business activity event type.");
        }

        if (query.ActorUserId is <= 0)
            throw new ArgumentException(
                "Actor user ID must be positive.");

        if (query.ComplaintId is <= 0)
            throw new ArgumentException(
                "Complaint ID must be positive.");

        if (query.SearchTerm?.Length > 100)
            throw new ArgumentException(
                "Search cannot exceed 100 characters.");

        if (query.FromUtc.HasValue &&
            query.ToUtcExclusive.HasValue &&
            query.FromUtc.Value >= query.ToUtcExclusive.Value)
        {
            throw new ArgumentException(
                "The start date must be before the end date.");
        }

        var (items, totalCount) =
            await _repository.SearchAsync(
                query,
                cancellationToken);

        var results = items.Select(activity =>
            new BusinessActivityDto
            {
                BusinessActivityEventId =
                    activity.BusinessActivityEventId,

                EventTypeId = (int)activity.EventType,
                EventType = activity.EventType.ToString(),

                OccurredAtUtc = DateTime.SpecifyKind(
                    activity.OccurredAtUtc,
                    DateTimeKind.Utc),

                ActorUserId = activity.ActorUserId,
                ActorName = activity.ActorName,
                ActorRole = activity.ActorRole,

                TargetUserId = activity.TargetUserId,
                TargetUserName = activity.TargetUserName,

                CustomerId = activity.CustomerId,
                CustomerName = activity.CustomerName,

                ComplaintId = activity.ComplaintId,
                TicketNumber = activity.TicketNumber,

                Title = activity.Title,
                Description = activity.Description,
                DetailsJson = activity.DetailsJson
            })
            .ToList();

        return new PagedBusinessActivityDto
        {
            Items = results,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)(
                ((long)totalCount + query.PageSize - 1)
                / query.PageSize)
        };
    }
}