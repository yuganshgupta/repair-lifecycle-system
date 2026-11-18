using Microsoft.EntityFrameworkCore;
using RepairSystem.Application.Contracts.BusinessActivity;
using RepairSystem.Application.Interfaces.BusinessActivity;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;
using RepairSystem.Infrastructure.Data;

namespace RepairSystem.Infrastructure.Repositories.BusinessActivity;

public sealed class BusinessActivityRepository
    : IBusinessActivityRepository
{
    private readonly RepairDbContext _context;

    public BusinessActivityRepository(
        RepairDbContext context)
    {
        _context = context;
    }

    public async Task<(
        IReadOnlyList<BusinessActivityEvent> Items,
        int TotalCount)> SearchAsync(
            BusinessActivityQuery query,
            CancellationToken cancellationToken)
    {
        IQueryable<BusinessActivityEvent> records =
            _context.BusinessActivityEvents.AsNoTracking();

        // Filter by activity type.
        if (query.EventType.HasValue)
        {
            var eventType =
                (BusinessActivityType)query.EventType.Value;

            records = records.Where(
                activity => activity.EventType == eventType);
        }

        // Filter by the user who performed the action.
        if (query.ActorUserId.HasValue)
        {
            var actorId = query.ActorUserId.Value;

            records = records.Where(
                activity => activity.ActorUserId == actorId);
        }

        // Filter by complaint.
        if (query.ComplaintId.HasValue)
        {
            var complaintId = query.ComplaintId.Value;

            records = records.Where(
                activity => activity.ComplaintId == complaintId);
        }

        // Filter by date range in UTC.
        if (query.FromUtc.HasValue)
        {
            var from = query.FromUtc.Value.UtcDateTime;

            records = records.Where(
                activity => activity.OccurredAtUtc >= from);
        }

        if (query.ToUtcExclusive.HasValue)
        {
            var to = query.ToUtcExclusive.Value.UtcDateTime;

            records = records.Where(
                activity => activity.OccurredAtUtc < to);
        }

        // Search across useful business information.
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.Trim();

            records = records.Where(activity =>
                activity.Title.Contains(term) ||
                activity.Description.Contains(term) ||
                activity.ActorName.Contains(term) ||
                (activity.TicketNumber != null &&
                    activity.TicketNumber.Contains(term)) ||
                (activity.CustomerName != null &&
                    activity.CustomerName.Contains(term)) ||
                (activity.TargetUserName != null &&
                    activity.TargetUserName.Contains(term)));
        }

        // Count before applying pagination.
        var totalCount = await records.CountAsync(
            cancellationToken);

        // Stable newest-first ordering.
        var items = await records
            .OrderByDescending(
                activity => activity.OccurredAtUtc)
            .ThenByDescending(
                activity => activity.BusinessActivityEventId)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}