
using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Application.Contracts.Users;
using RepairSystem.Application.Interfaces.Auth;

namespace RepairSystem.Api.Tests;

public sealed class FakeAuthService : IAuthService
{
    public int CreateUserCallCount { get; private set; }

    public CreateUserRequestDto? LastCreatedRequest { get; private set; }

    public int GetUsersCallCount { get; private set; }

    public IReadOnlyList<AdminUserDto> Users { get; set; }
        = Array.Empty<AdminUserDto>();

    public int UpdateUserCallCount { get; private set; }

    public int? LastUpdatedUserId { get; private set; }

    public UpdateUserRequestDto? LastUpdateRequest { get; private set; }

    public UserUpdateResult UpdateResult { get; set; }
        = UserUpdateResult.Updated;

    public UserStatusUpdateResult StatusUpdateResult { get; set; }
    = UserStatusUpdateResult.Updated;

    public int StatusUpdateCallCount { get; private set; }

    public int? LastStatusTargetUserId { get; private set; }

    public int? LastStatusRequestingAdminId { get; private set; }

    public bool? LastRequestedActiveStatus { get; private set; }

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

    public Task<IReadOnlyList<AdminUserDto>> GetUsersAsync()
    {
        GetUsersCallCount++;

        return Task.FromResult(Users);
    }

    public Task<UserUpdateResult> UpdateUserAsync(
    int userId,
    UpdateUserRequestDto request)
    {
        UpdateUserCallCount++;

        LastUpdatedUserId = userId;
        LastUpdateRequest = request;

        return Task.FromResult(UpdateResult);
    }


    public Task<UserStatusUpdateResult> UpdateUserStatusAsync(
        int requestingAdminId,
        int targetUserId,
        bool isActive)
    {
        StatusUpdateCallCount++;
        LastStatusRequestingAdminId = requestingAdminId;
        LastStatusTargetUserId = targetUserId;
        LastRequestedActiveStatus = isActive;

        return Task.FromResult(StatusUpdateResult);
    }
}
