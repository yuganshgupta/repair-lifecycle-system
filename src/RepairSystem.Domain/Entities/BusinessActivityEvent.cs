using RepairSystem.Domain.Enums;

namespace RepairSystem.Domain.Entities;

/// <summary>
/// Immutable record of an important business operation.
/// User and complaint IDs are historical references, not foreign keys.
/// This preserves the activity record independently of related records.
/// </summary>
public sealed class BusinessActivityEvent
{
    private BusinessActivityEvent()
    {
        // Used by Entity Framework Core.
    }

    public int BusinessActivityEventId { get; private set; }

    public BusinessActivityType EventType { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    // The user or system responsible for the operation.
    public int? ActorUserId { get; private set; }

    public string ActorName { get; private set; } = string.Empty;

    public string ActorRole { get; private set; } = string.Empty;

    // The account affected by the operation, where applicable.
    public int? TargetUserId { get; private set; }

    public string? TargetUserName { get; private set; }

    // Related customer and complaint information.
    public int? CustomerId { get; private set; }

    public string? CustomerName { get; private set; }

    public int? ComplaintId { get; private set; }

    public string? TicketNumber { get; private set; }

    // Human-readable information for the administrator.
    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    // Optional structured information containing only approved,
    // non-secret business data.
    public string? DetailsJson { get; private set; }

    public static BusinessActivityEvent Create(
        BusinessActivityType eventType,
        string actorName,
        string actorRole,
        string title,
        string description,
        int? actorUserId = null,
        int? targetUserId = null,
        string? targetUserName = null,
        int? customerId = null,
        string? customerName = null,
        int? complaintId = null,
        string? ticketNumber = null,
        string? detailsJson = null)
    {
        if (!Enum.IsDefined(eventType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(eventType),
                "The business activity type is invalid.");
        }

        ValidateOptionalId(actorUserId, nameof(actorUserId));
        ValidateOptionalId(targetUserId, nameof(targetUserId));
        ValidateOptionalId(customerId, nameof(customerId));
        ValidateOptionalId(complaintId, nameof(complaintId));

        return new BusinessActivityEvent
        {
            EventType = eventType,

            OccurredAtUtc = DateTime.UtcNow,

            ActorUserId = actorUserId,
            ActorName = RequiredText(
                actorName,
                nameof(actorName),
                120),
            ActorRole = RequiredText(
                actorRole,
                nameof(actorRole),
                32),

            TargetUserId = targetUserId,
            TargetUserName = OptionalText(
                targetUserName,
                nameof(targetUserName),
                120),

            CustomerId = customerId,
            CustomerName = OptionalText(
                customerName,
                nameof(customerName),
                120),

            ComplaintId = complaintId,
            TicketNumber = OptionalText(
                ticketNumber,
                nameof(ticketNumber),
                64),

            Title = RequiredText(
                title,
                nameof(title),
                120),
            Description = RequiredText(
                description,
                nameof(description),
                1000),

            DetailsJson = OptionalText(
                detailsJson,
                nameof(detailsJson),
                8000)
        };
    }

    private static void ValidateOptionalId(
        int? value,
        string parameterName)
    {
        if (value is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "An identifier must be greater than zero.");
        }
    }

    private static string RequiredText(
        string? value,
        string parameterName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{parameterName} is required.",
                parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }

    private static string? OptionalText(
        string? value,
        string parameterName,
        int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{parameterName} cannot exceed {maximumLength} characters.",
                parameterName);
        }

        return normalized;
    }
}