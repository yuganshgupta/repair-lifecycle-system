using RepairSystem.Application.Interfaces.Users;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Api.Tests;

public sealed class FakeTokenAccountRepository : IUserRepository
{
    public bool AllowSyntheticAccounts { get; set; } = true;
    public Dictionary<int, (bool Active, UserRole Role)> Accounts { get; } = new();

    public Task<bool> IsActiveInRoleAsync(int userId, UserRole role, CancellationToken ct)
        => Task.FromResult(AllowSyntheticAccounts ||
            (Accounts.TryGetValue(userId, out var account) && account.Active && account.Role == role));

    public Task<bool> EmailExistsAsync(string email) => throw new NotSupportedException();
    public Task AddAsync(User user) => throw new NotSupportedException();
    public Task<User?> FindByEmailAsync(string email) => throw new NotSupportedException();
    public Task<IReadOnlyList<User>> GetAllAsync() => throw new NotSupportedException();
    public Task<User?> GetByIdAsync(int userId) => throw new NotSupportedException();
    public Task UpdateAsync(User user) => throw new NotSupportedException();
    public Task<int> CountActiveAdminsAsync() => throw new NotSupportedException();
    public Task<bool> HasOpenComplaintsAsync(int userId, UserRole role) => throw new NotSupportedException();
    public Task<T> ExecuteAccountChangeAsync<T>(Func<Task<T>> operation) => throw new NotSupportedException();
}
