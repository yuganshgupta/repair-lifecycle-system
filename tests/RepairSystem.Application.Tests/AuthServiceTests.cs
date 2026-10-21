using Microsoft.AspNetCore.Identity;
using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Application.Services.Auth;
using RepairSystem.Application.Interfaces.Users;
using RepairSystem.Domain.Entities;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Application.Tests;

[TestClass]
public sealed class AuthServiceTests
{
    [TestMethod]
    public async Task RegisterAsync_ReturnsFalse_WhenEmailAlreadyExists()
    {
        var u1 = new FakeUserRepository();
        u1.EmailExists = true;

        var passwordHasher = new PasswordHasher<User>();
        var authService = new AuthService(u1, passwordHasher);

        var request = new RegisterRequest
        {
            FullName = "Test",
            Email = "test@test.com",
            Password = "test1234"
        };

        var result = await authService.RegisterAsync(request);
        Assert.IsFalse(result);
        Assert.IsNull(u1.AddedUser);
    }

    [TestMethod]
    public async Task RegisterAsync_ReturnsTrue_WhenEmailDoesNotExist()
    {
        var u1 = new FakeUserRepository();
        u1.EmailExists = false;

        var passwordHasher = new PasswordHasher<User>();
        var authService = new AuthService(u1, passwordHasher);

        var request = new RegisterRequest
        {
            FullName = "Test",
            Email = "test@test.com",
            Password = "test1234"
        };

        var result = await authService.RegisterAsync(request);
        Assert.IsTrue(result);
        Assert.IsNotNull(u1.AddedUser);
        Assert.AreNotEqual(request.Password,u1.AddedUser.PasswordHash);
        Assert.AreEqual(UserRole.Customer,u1.AddedUser.Role);
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? UserToFind { get; set; }

        public bool EmailExists { get; set; }

        public User? AddedUser { get; private set; }

        public Task<bool> EmailExistsAsync(string email)
        {
            return Task.FromResult(EmailExists);
        }

        public Task AddAsync(User user)
        {
            AddedUser = user;
            return Task.CompletedTask;
        }

        public Task<User?> FindByEmailAsync(string email)
        {
            return Task.FromResult(UserToFind);
        }
    }
}
