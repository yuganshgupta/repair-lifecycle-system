using RepairSystem.Domain.Entities;

namespace RepairSystem.Application.Interfaces.Users;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email);
    Task AddAsync(User user);

    Task<User?> FindByEmailAsync(string email);

}
