
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairSystem.Api.Security;
using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Application.Interfaces.Auth;

namespace RepairSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
	private readonly IAuthService _authService;
	private readonly IAccessTokenGenerator _accessTokenGenerator;

	public AuthController(
		IAuthService authService,
		IAccessTokenGenerator accessTokenGenerator)
	{
		_authService = authService;
		_accessTokenGenerator = accessTokenGenerator;
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
		// Verify the user's credentials.
		var user = await _authService.LoginAsync(request);

		if (user is null)
		{
			return Unauthorized(new
			{
				message = "Invalid email or password."
			});
		}

		// Delegate JWT creation to the token generator.
		var generatedToken = _accessTokenGenerator.Generate(user);

		// Return the existing API response contract.
		return Ok(new LoginResponseDto
		{
			AccessToken = generatedToken.AccessToken,
			TokenType = "Bearer",
			ExpiresInMinutes = generatedToken.ExpiresInMinutes,
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
