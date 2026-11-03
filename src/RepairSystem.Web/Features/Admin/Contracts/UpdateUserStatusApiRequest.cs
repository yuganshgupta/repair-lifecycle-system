using System.Text.Json.Serialization;

namespace RepairSystem.Web.Features.Admin.Contracts;

public sealed class UpdateUserStatusApiRequest
{
    [JsonPropertyName("isActive")]
    public bool IsActive { get; init; }
}