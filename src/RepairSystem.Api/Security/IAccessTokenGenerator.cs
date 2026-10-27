using RepairSystem.Application.Contracts.Auth;

namespace RepairSystem.Api.Security;

public interface IAccessTokenGenerator
{
    AccessTokenGenerationResult Generate(LoginResultDto user);
}