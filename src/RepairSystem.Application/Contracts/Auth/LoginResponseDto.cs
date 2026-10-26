using System.Text.Json.Serialization;

namespace RepairSystem.Application.Contracts.Auth;

public sealed class LoginResponseDto
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("tokenType")]
    public string TokenType { get; set; } = "Bearer";

    // The existing API returns this lifetime in minutes.
    // Keep the external JSON name "expiresIn" for compatibility.
    [JsonPropertyName("expiresIn")]
    public int ExpiresInMinutes { get; set; }

    [JsonPropertyName("user")]
    public LoginUserDto User { get; set; } = new();
}