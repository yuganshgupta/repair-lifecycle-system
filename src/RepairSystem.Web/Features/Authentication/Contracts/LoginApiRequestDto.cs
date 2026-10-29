using System.Text.Json.Serialization;

namespace RepairSystem.Web.Features.Authentication.Contracts;

public sealed class LoginApiRequestDto
{
    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; init; } = string.Empty;
}
