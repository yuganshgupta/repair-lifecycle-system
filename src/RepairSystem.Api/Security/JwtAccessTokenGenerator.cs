
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using RepairSystem.Application.Contracts.Auth;

namespace RepairSystem.Api.Security;

public sealed class JwtAccessTokenGenerator : IAccessTokenGenerator
{
    private readonly SymmetricSecurityKey _signingKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenMinutes;

    public JwtAccessTokenGenerator(IConfiguration configuration)
    {
        var jwtKeyBase64 = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "JWT signing key was not found.");

        var signingKeyBytes = Convert.FromBase64String(jwtKeyBase64);
        _signingKey = new SymmetricSecurityKey(signingKeyBytes);

        _issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer was not found.");

        _audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience was not found.");

        if (!int.TryParse(
                configuration["Jwt:AccessTokenMinutes"],
                out _accessTokenMinutes) ||
            _accessTokenMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JWT access token lifetime must be a positive integer.");
        }
    }

    public AccessTokenGenerationResult Generate(LoginResultDto user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenMinutes),
            signingCredentials: new SigningCredentials(
                _signingKey,
                SecurityAlgorithms.HmacSha256));

        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new AccessTokenGenerationResult(
            accessToken,
            _accessTokenMinutes);
    }
}
