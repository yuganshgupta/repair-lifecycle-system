
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairSystem.Web.Services.Authentication;

namespace RepairSystem.Web.Features.Dashboard;

[Authorize]
[Route("repair-evidence")]
public sealed class RepairEvidenceController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IApiTokenSessionStore _tokenStore;

    public RepairEvidenceController(
        IHttpClientFactory httpClientFactory,
        IApiTokenSessionStore tokenStore)
    {
        _httpClientFactory = httpClientFactory;
        _tokenStore = tokenStore;
    }

    [HttpGet("{complaintId:int:min(1)}/{attachmentId:int:min(1)}")]
    public async Task<IActionResult> Get(
        int complaintId,
        int attachmentId,
        CancellationToken ct)
    {
        var sessionId = User
            .FindFirst(
                WebAuthenticationDefaults.SessionIdClaimType)
            ?.Value;

        if (string.IsNullOrWhiteSpace(sessionId) ||
            !_tokenStore.TryGet(sessionId, out var session) ||
            session is null ||
            session.AccessTokenExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return Unauthorized();
        }

        using var client =
            _httpClientFactory.CreateClient("RepairApi");

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/repair-workflow/{complaintId}/attachments/{attachmentId}");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer", session.AccessToken);

        try
        {
            using var response = await client.SendAsync(
                request, ct);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode);
            }

            var contentType =
                response.Content.Headers.ContentType?.MediaType;

            if (contentType is not ("image/jpeg" or "image/png"))
            {
                return NotFound();
            }

            var bytes = await response.Content
                .ReadAsByteArrayAsync(ct);

            if (bytes.LongLength > 20L * 1024 * 1024)
            {
                return StatusCode(413);
            }

            Response.Headers.CacheControl = "no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return File(bytes, contentType);
        }
        catch (HttpRequestException)
        {
            return StatusCode(503);
        }
    }
}
