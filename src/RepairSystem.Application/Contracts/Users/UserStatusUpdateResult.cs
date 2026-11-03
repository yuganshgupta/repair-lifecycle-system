namespace RepairSystem.Application.Contracts.Users;

public enum UserStatusUpdateResult
{
    Updated,
    NotFound,
    AlreadyInRequestedState,
    CannotDeactivateOwnAccount,
    CannotDeactivateLastActiveAdmin
}