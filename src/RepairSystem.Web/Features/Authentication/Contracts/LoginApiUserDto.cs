using System.Text.Json.Serialization;

namespace RepairSystem.Web.Features.Authentication.Contracts;

public sealed class LoginApiUserDto
{
    [JsonPropertyName("userId")]
    public int UserId { get; init; }

    [JsonPropertyName("fullName")]
    public string FullName { get; init; } = string.Empty;

    [JsonPropertyName("email")]
    public string Email { get; init; } = string.Empty;

    [JsonPropertyName("role")]
    public string Role { get; init; } = string.Empty;
}
