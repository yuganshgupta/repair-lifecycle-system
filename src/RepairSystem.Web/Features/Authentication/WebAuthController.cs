
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
                new
                {
                    email = email.Trim(),
                    password
                });

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Redirect("/login?error=invalid");
            }

            if (!response.IsSuccessStatusCode)
            {
                return Redirect("/login?error=api");
            }

            using var document =
                await response.Content.ReadFromJsonAsync<JsonDocument>();

            if (document is null ||
                document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Redirect("/login?error=api-response");
            }

            var root = document.RootElement;

            if (!TryGetString(root, "accessToken", out var accessToken) ||
                !TryGetString(root, "tokenType", out var tokenType) ||
                !string.Equals(
                    tokenType,
                    "Bearer",
                    StringComparison.OrdinalIgnoreCase) ||
                !TryGetInt32(root, "expiresIn", out var expiresInMinutes) ||
                expiresInMinutes <= 0 ||
                !root.TryGetProperty("user", out var userElement) ||
                userElement.ValueKind != JsonValueKind.Object)
            {
                return Redirect("/login?error=api-response");
            }

            if (!TryGetInt32(userElement, "userId", out var userId) ||
                userId <= 0 ||
                !TryGetString(userElement, "fullName", out var fullName) ||
                !TryGetString(userElement, "email", out var userEmail) ||
                !TryGetString(userElement, "role", out var role))
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
                    AccessToken: accessToken,
                    AccessTokenExpiresAtUtc: now.AddMinutes(expiresInMinutes),
                    SessionExpiresAtUtc: sessionExpiresAtUtc));

            var claims = new List<Claim>
            {
                new(
                    ClaimTypes.NameIdentifier,
                    userId.ToString(CultureInfo.InvariantCulture)),
                new(ClaimTypes.Name, fullName),
                new(ClaimTypes.Email, userEmail),
                new(ClaimTypes.Role, role),
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

    private static bool TryGetString(
        JsonElement element,
        string propertyName,
        out string value)
    {
        value = string.Empty;

        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;

        return !string.IsNullOrWhiteSpace(value);
    }

    private static bool TryGetInt32(
        JsonElement element,
        string propertyName,
        out int value)
    {
        value = 0;

        return element.ValueKind == JsonValueKind.Object &&
               element.TryGetProperty(propertyName, out var property) &&
               property.ValueKind == JsonValueKind.Number &&
               property.TryGetInt32(out value);
    }
}
