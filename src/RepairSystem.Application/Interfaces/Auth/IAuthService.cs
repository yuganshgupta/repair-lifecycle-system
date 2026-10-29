using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Domain.Entities;

namespace RepairSystem.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterRequestDto request);

    Task<LoginResultDto?> LoginAsync(LoginRequestDto request);

    Task<bool> CreateUserAsync(CreateUserRequestDto request);
}
