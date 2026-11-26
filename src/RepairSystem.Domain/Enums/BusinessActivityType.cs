namespace RepairSystem.Domain.Enums;

/// <summary>
/// Identifies a business operation recorded in the activity log.
/// Numeric values are permanent: append new values, never reorder them.
/// </summary>
public enum BusinessActivityType
{
    CustomerRegistered = 1,
    UserAccountCreated = 2,
    UserDetailsUpdated = 3,
    UserActivated = 4,
    UserDeactivated = 5,

    ComplaintSubmitted = 6,
    TechnicianAssigned = 7,
    DiagnosisStarted = 8,

    QuotationSubmitted = 9,
    QuotationAccepted = 10,
    QuotationRejected = 11,

    RepairStarted = 12,
    RepairCompleted = 13,
    ComplaintCancelled = 14,
    TechnicianClaimed = 15,
    TechnicianTransferred = 16,
    TechnicianReassigned = 17
}