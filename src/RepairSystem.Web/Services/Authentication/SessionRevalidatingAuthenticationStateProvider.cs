using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace RepairSystem.Web.Services.Authentication;

public sealed class SessionRevalidatingAuthenticationStateProvider
    : RevalidatingServerAuthenticationStateProvider
{
    private readonly IApiTokenSessionStore _sessionStore;

    public SessionRevalidatingAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IApiTokenSessionStore sessionStore)
        : base(loggerFactory)
    {
        _sessionStore = sessionStore;
    }

    protected override TimeSpan RevalidationInterval =>
        TimeSpan.FromSeconds(5);

    protected override Task<bool> ValidateAuthenticationStateAsync(
        AuthenticationState authenticationState,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sessionId = authenticationState.User
            .FindFirst(WebAuthenticationDefaults.SessionIdClaimType)
            ?.Value;

        var isSessionValid =
            !string.IsNullOrWhiteSpace(sessionId) &&
            _sessionStore.TryGet(sessionId, out _);

        return Task.FromResult(isSessionValid);
    }
}