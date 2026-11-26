namespace RepairSystem.Application.Contracts.Users;

public sealed class AccountChangeConflictException(Exception innerException)
    : Exception("A competing account change occurred. Refresh and try again.", innerException)
{
}
