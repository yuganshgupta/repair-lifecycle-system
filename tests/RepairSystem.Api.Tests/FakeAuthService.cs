
using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Application.Interfaces.Auth;

namespace RepairSystem.Api.Tests;

public sealed class FakeAuthService : IAuthService
{
    public int CreateUserCallCount { get; private set; }

    public CreateUserRequestDto? LastCreatedRequest { get; private set; }

    public Task<bool> RegisterAsync(RegisterRequestDto request)
        => Task.FromResult(false);

    public Task<LoginResultDto?> LoginAsync(LoginRequestDto request)
        => Task.FromResult<LoginResultDto?>(null);

    public Task<bool> CreateUserAsync(CreateUserRequestDto request)
    {
        CreateUserCallCount++;
        LastCreatedRequest = request;

        return Task.FromResult(true);
    }
}
