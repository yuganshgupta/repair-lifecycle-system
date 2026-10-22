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

    [TestMethod]
    public async Task LoginAsync_ReturnsUser_WhenCredentialsAreValid()
    {
        var u1 = new FakeUserRepository();
        var passwordHasher = new PasswordHasher<User>();

        var user = new User
        {
            FullName = "Test",
            Email = "test@test.com",
            IsActive = true,
            Role = UserRole.Customer
        };

        user.PasswordHash = passwordHasher.HashPassword(user,"test1234");
        u1.UserToFind = user;

        var authService = new AuthService(u1, passwordHasher);

        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "test1234"
        };

        var result = await authService.LoginAsync(request);
        Assert.IsNotNull(result);
        Assert.AreEqual("test@test.com", result.Email);
        Assert.AreEqual("Test", result.FullName);
        Assert.AreEqual(UserRole.Customer, result.Role);
    }


    [TestMethod]
    public async Task LoginAsync_ReturnsNull_WhenPasswordIsIncorrect()
    {
        var repository = new FakeUserRepository();
        var passwordHasher = new PasswordHasher<User>();

        var user = new User
        {
            FullName = "Test",
            Email = "test@test.com",
            IsActive = true,
            Role = UserRole.Customer
        };

        user.PasswordHash = passwordHasher.HashPassword(user, "test1234");
        repository.UserToFind = user;

        var authService = new AuthService(repository, passwordHasher);

        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "wrongpassword"
        };

        var result = await authService.LoginAsync(request);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task LoginAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        var repository = new FakeUserRepository();
        var passwordHasher = new PasswordHasher<User>();
        var authService = new AuthService(repository, passwordHasher);

        var request = new LoginRequest
        {
            Email = "missing@test.com",
            Password = "test1234"
        };

        var result = await authService.LoginAsync(request);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task LoginAsync_ReturnsNull_WhenUserIsInactive()
    {
        var repository = new FakeUserRepository();
        var passwordHasher = new PasswordHasher<User>();

        var user = new User
        {
            FullName = "Test",
            Email = "test@test.com",
            IsActive = false,
            Role = UserRole.Customer
        };

        user.PasswordHash = passwordHasher.HashPassword(user, "test1234");
        repository.UserToFind = user;

        var authService = new AuthService(repository, passwordHasher);

        var request = new LoginRequest
        {
            Email = "test@test.com",
            Password = "test1234"
        };

        var result = await authService.LoginAsync(request);

        Assert.IsNull(result);
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
