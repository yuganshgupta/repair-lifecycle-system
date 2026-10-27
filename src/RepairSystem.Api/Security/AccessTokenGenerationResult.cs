namespace RepairSystem.Api.Security;

public sealed record AccessTokenGenerationResult(
    string AccessToken,
    int ExpiresInMinutes);