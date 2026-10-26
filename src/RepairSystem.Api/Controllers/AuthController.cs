using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Application.Interfaces.Auth;

namespace RepairSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService authService,
        IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto request)
    {
        var registered = await _authService.RegisterAsync(request);

        if (!registered)
        {
            return Conflict(new
            {
                message = "An account with this email already exists."
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Registration successful."
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequestDto request)
    {
        var user = await _authService.LoginAsync(request);

        if (user is null)
        {
            return Unauthorized(new
            {
                message = "Invalid email or password."
            });
        }

        // Get the JWT settings that we configured earlier
        var jwtKeyBase64 = _configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "JWT signing key was not found.");

        var issuer = _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer was not found.");

        var audience = _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience was not found.");

        if (!int.TryParse(
                _configuration["Jwt:AccessTokenMinutes"],
                out var accessTokenMinutes) ||
            accessTokenMinutes <= 0)
        {
            throw new InvalidOperationException(
                "JWT access token lifetime must be a positive integer.");
        }

        var signingKey = new SymmetricSecurityKey(
            Convert.FromBase64String(jwtKeyBase64));

        // Add the user information that protected routes can use
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        // Create a token that expires after the configured time
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(accessTokenMinutes),
            signingCredentials: new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256));

        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

		return Ok(new LoginResponseDto
		{
			AccessToken = accessToken,
			TokenType = "Bearer",
			ExpiresInMinutes = accessTokenMinutes,
			User = new LoginUserDto
			{
				UserId = user.UserId,
				FullName = user.FullName,
				Email = user.Email,
				Role = user.Role.ToString()
			}
		});
	}
}