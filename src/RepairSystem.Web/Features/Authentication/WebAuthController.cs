using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairSystem.Web.Features.Authentication.Contracts;
using RepairSystem.Web.Services.Authentication;

namespace RepairSystem.Web.Features.Authentication;

[Route("auth")]
public sealed class WebAuthController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiTokenSessionStore _tokenStore;

    public WebAuthController(
        IHttpClientFactory httpClientFactory,
        IApiTokenSessionStore tokenStore)
    {
        _httpClientFactory = httpClientFactory;
        _tokenStore = tokenStore;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        [FromForm] string email,
        [FromForm] string password)
    {
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrEmpty(password))
        {
            return Redirect("/login?error=invalid");
        }

        try
        {
            var client = _httpClientFactory.CreateClient("RepairApi");

            using var response = await client.PostAsJsonAsync(
                "api/auth/login",
                new LoginApiRequestDto
                {
                    Email = email.Trim(),
                    Password = password
                });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Redirect("/login?error=invalid");
            }

            if (!response.IsSuccessStatusCode)
            {
                return Redirect("/login?error=api");
            }

            var loginResponse =
                await response.Content.ReadFromJsonAsync<LoginApiResponseDto>();
            var user = loginResponse?.User;

            if (loginResponse is null ||
                string.IsNullOrWhiteSpace(loginResponse.AccessToken) ||
                !string.Equals(
                    loginResponse.TokenType,
                    "Bearer",
                    StringComparison.OrdinalIgnoreCase) ||
                loginResponse.ExpiresInMinutes <= 0 ||
                user is null ||
                user.UserId <= 0 ||
                string.IsNullOrWhiteSpace(user.FullName) ||
                string.IsNullOrWhiteSpace(user.Email) ||
                string.IsNullOrWhiteSpace(user.Role))
            {
                return Redirect("/login?error=api-response");
            }

            var now = DateTimeOffset.UtcNow;
            var sessionExpiresAtUtc = now.AddHours(
                WebAuthenticationDefaults.SessionLifetimeHours);
            var sessionId = Guid.NewGuid().ToString("N");

            // Keep the API JWT on the server, not in browser storage.
            _tokenStore.Save(
                sessionId,
                new ApiTokenSession(
                    AccessToken: loginResponse.AccessToken,
                    AccessTokenExpiresAtUtc: now.AddMinutes(loginResponse.ExpiresInMinutes),
                    SessionExpiresAtUtc: sessionExpiresAtUtc));

            var claims = new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    user.UserId.ToString(CultureInfo.InvariantCulture)),
                new(ClaimTypes.Name, user.FullName),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                new(
                    WebAuthenticationDefaults.SessionIdClaimType,
                    sessionId)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = sessionExpiresAtUtc,
                    AllowRefresh = false
                });

            return LocalRedirect("/dashboard");
        }
        catch (HttpRequestException)
        {
            return Redirect("/login?error=api-unavailable");
        }
        catch (TaskCanceledException)
        {
            return Redirect("/login?error=api-unavailable");
        }
        catch (JsonException)
        {
            return Redirect("/login?error=api-response");
        }
    }

    [Authorize]
    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var sessionId = User
            .FindFirst(WebAuthenticationDefaults.SessionIdClaimType)
            ?.Value;

        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            _tokenStore.Remove(sessionId);
        }

        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Redirect("/login?loggedOut=true");
    }
}
