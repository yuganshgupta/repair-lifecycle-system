using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Domain.Entities;

namespace RepairSystem.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterRequest request);

    Task<LoginResult?> LoginAsync(LoginRequest request);
}
