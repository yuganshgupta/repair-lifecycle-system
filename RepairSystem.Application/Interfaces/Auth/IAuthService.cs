using RepairSystem.Application.Contracts.Auth;

namespace RepairSystem.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<bool> RegisterAsync(RegisterRequest request);
}
