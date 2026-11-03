using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Application.Contracts.Users;
using RepairSystem.Domain.Entities;

namespace RepairSystem.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterRequestDto request);

    Task<LoginResultDto?> LoginAsync(LoginRequestDto request);

    Task<bool> CreateUserAsync(CreateUserRequestDto request);

    Task<IReadOnlyList<AdminUserDto>> GetUsersAsync();

    Task<UserUpdateResult> UpdateUserAsync(
    int userId,
    UpdateUserRequestDto request);
}
