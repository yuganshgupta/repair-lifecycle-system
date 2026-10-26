
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace RepairSystem.Web.Services.Authentication;

public sealed class RepairAuthenticationStateProvider
    : AuthenticationStateProvider
{
    private ClaimsPrincipal _currentUser = new(
        new ClaimsIdentity());

    public override Task<AuthenticationState>
        GetAuthenticationStateAsync()
    {
        return Task.FromResult(
            new AuthenticationState(_currentUser));
    }

    public void SignIn(
        int userId,
        string fullName,
        string email,
        string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, fullName),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "RepairSystem");

        _currentUser = new ClaimsPrincipal(identity);

        NotifyAuthenticationStateChanged(
            GetAuthenticationStateAsync());
    }

    public void SignOut()
    {
        _currentUser = new ClaimsPrincipal(
            new ClaimsIdentity());

        NotifyAuthenticationStateChanged(
            GetAuthenticationStateAsync());
    }
}
