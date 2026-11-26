using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Application.Interfaces.Users;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);

    Task AddAsync(User user);

    Task<User?> FindByEmailAsync(string email);

    Task<IReadOnlyList<User>> GetAllAsync();

    Task<User?> GetByIdAsync(int userId);

    Task UpdateAsync(User user);

    Task<int> CountActiveAdminsAsync();

    Task<bool> IsActiveInRoleAsync(int userId, UserRole role, CancellationToken ct);

    Task<bool> HasOpenComplaintsAsync(int userId, UserRole role);

    // All reads and writes for an account change share one serializable transaction.
    Task<T> ExecuteAccountChangeAsync<T>(Func<Task<T>> operation);
}