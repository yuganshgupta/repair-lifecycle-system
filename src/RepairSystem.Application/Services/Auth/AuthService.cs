using Microsoft.AspNetCore.Identity;
using RepairSystem.Application.Interfaces.Users;
using RepairSystem.Domain.Entities;
using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Application.Interfaces.Auth;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(IUserRepository userRepository , IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> RegisterAsync(RegisterRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Check if someone has already registered with this email
        if (await _userRepository.EmailExistsAsync(email))
        {
            return false;
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
        ? null
        : request.PhoneNumber.Trim(),
            Role = UserRole.Customer
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        await _userRepository.AddAsync(user);

        return true;

    }


    public async Task<LoginResultDto?> LoginAsync(LoginRequestDto request)
    {
        // Find the user using the email they entered
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.FindByEmailAsync(email);

        // Don't allow login if the account doesn't exist or is inactive
        if (user is null || !user.IsActive)
        {
            return null;
        }

        // Check the password against the stored hash
        var passwordResult = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        // Return only the information needed to create the login token
        return new LoginResultDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        };
    }

    public async Task<bool> CreateUserAsync(
    CreateUserRequestDto request)
    {
        var role = request.Role switch
        {
            nameof(UserRole.Customer) => UserRole.Customer,
            nameof(UserRole.Technician) => UserRole.Technician,
            nameof(UserRole.Admin) => UserRole.Admin,
            _ => (UserRole?)null
        };

        if (role is null)
        {
            return false;
        }

        var email = request.Email.Trim().ToLowerInvariant();

        if (await _userRepository.EmailExistsAsync(email))
        {
            return false;
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            Role = role.Value,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            request.Password);

        await _userRepository.AddAsync(user);

        return true;
    }



}
