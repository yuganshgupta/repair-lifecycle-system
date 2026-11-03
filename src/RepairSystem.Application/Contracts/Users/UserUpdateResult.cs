namespace RepairSystem.Application.Contracts.Users;

public enum UserUpdateResult
{
    Updated,
    NotFound,
    EmailAlreadyExists,
    InvalidRole
}