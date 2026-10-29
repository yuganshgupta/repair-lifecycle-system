using System.Text.Json.Serialization;

namespace RepairSystem.Web.Features.Authentication.Contracts;

public sealed class LoginApiResponseDto
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("tokenType")]
    public string TokenType { get; init; } = string.Empty;

    // The API contract expresses the access-token lifetime in minutes.
    [JsonPropertyName("expiresIn")]
    public int ExpiresInMinutes { get; init; }

    [JsonPropertyName("user")]
    public LoginApiUserDto? User { get; init; }
}
