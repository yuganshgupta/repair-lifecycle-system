using RepairSystem.Domain.Entities;

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
}