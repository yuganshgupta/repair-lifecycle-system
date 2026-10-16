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

    public async Task<bool> RegisterAsync(RegisterRequest request)
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



}
