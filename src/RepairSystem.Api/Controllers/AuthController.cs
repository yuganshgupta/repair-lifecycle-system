
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RepairSystem.Api.Security;
using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Application.Contracts.Users;
using RepairSystem.Application.Interfaces.Auth;
using System.Security.Claims;

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

    [Authorize(Roles = "Admin")]
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(
    [FromBody] CreateUserRequestDto request)
    {
        var created = await _authService.CreateUserAsync(request);

        if (!created)
        {
            return Conflict(new
            {
                message = "An account with this email already exists, " +
                          "or the requested role is invalid."
            });
        }

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "User account created successfully."
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

    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    public async Task<ActionResult<IReadOnlyList<AdminUserDto>>> GetUsers()
    {
        var users = await _authService.GetUsersAsync();

        return Ok(users);
    }


    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Roles = "Admin")]
    [HttpPut("users/{userId:int}")]
    public async Task<IActionResult> UpdateUser(
    int userId,
    [FromBody] UpdateUserRequestDto request)
    {
        var result = await _authService.UpdateUserAsync(
            userId,
            request);

        return result switch
        {
            UserUpdateResult.Updated => NoContent(),

            UserUpdateResult.NotFound => NotFound(new
            {
                message = "User account was not found."
            }),

            UserUpdateResult.EmailAlreadyExists => Conflict(new
            {
                message = "Another account already uses this email address."
            }),

            UserUpdateResult.InvalidRole => BadRequest(new
            {
                message = "The requested role is invalid."
            }),

            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("users/{userId:int}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateUserStatus(
    int userId,
    [FromBody] UpdateUserStatusRequestDto request)
    {
        var adminIdValue = User.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(adminIdValue, out var adminId) || adminId <= 0)
            return Unauthorized();

        var result = await _authService.UpdateUserStatusAsync(
            adminId,
            userId,
            request.IsActive!.Value);

        return result switch
        {
            UserStatusUpdateResult.Updated => NoContent(),
            UserStatusUpdateResult.AlreadyInRequestedState => NoContent(),

            UserStatusUpdateResult.NotFound => NotFound(),

            UserStatusUpdateResult.CannotDeactivateOwnAccount =>
                Conflict(new { message = "You cannot deactivate your own account." }),

            UserStatusUpdateResult.CannotDeactivateLastActiveAdmin =>
                Conflict(new { message = "The last active Admin account cannot be deactivated." }),

            _ => BadRequest()
        };
    }
}
