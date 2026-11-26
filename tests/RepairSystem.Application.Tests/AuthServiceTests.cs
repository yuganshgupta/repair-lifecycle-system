using Microsoft.AspNetCore.Identity;
using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Application.Contracts.Users;
using RepairSystem.Application.Interfaces.Users;
using RepairSystem.Application.Services.Auth;
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

        var request = new RegisterRequestDto
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

        var request = new RegisterRequestDto
        {
            FullName = "Test",
            Email = "test@test.com",
            Password = "test1234"
        };

        var result = await authService.RegisterAsync(request);
        Assert.IsTrue(result);
        Assert.IsNotNull(u1.AddedUser);
        Assert.AreNotEqual(request.Password, u1.AddedUser.PasswordHash);
        Assert.AreEqual(UserRole.Customer, u1.AddedUser.Role);
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

        user.PasswordHash = passwordHasher.HashPassword(user, "test1234");
        u1.UserToFind = user;

        var authService = new AuthService(u1, passwordHasher);

        var request = new LoginRequestDto
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

        var request = new LoginRequestDto
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

        var request = new LoginRequestDto
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

        var request = new LoginRequestDto
        {
            Email = "test@test.com",
            Password = "test1234"
        };

        var result = await authService.LoginAsync(request);

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task CreateUserAsync_CreatesTechnician_WhenRequestIsValid()
    {
        var repository = new FakeUserRepository();
        var passwordHasher = new PasswordHasher<User>();
        var authService = new AuthService(repository, passwordHasher);

        var request = new CreateUserRequestDto
        {
            FullName = "Test Technician",
            Email = "technician@test.com",
            Password = "technician123",
            Role = "Technician"
        };

        var result = await authService.CreateUserAsync(request);

        Assert.IsTrue(result);
        Assert.IsNotNull(repository.AddedUser);
        Assert.AreEqual(UserRole.Technician, repository.AddedUser.Role);
        Assert.AreEqual(
            "technician@test.com",
            repository.AddedUser.Email);
        Assert.AreNotEqual(
            request.Password,
            repository.AddedUser.PasswordHash);
    }

    [TestMethod]
    public async Task CreateUserAsync_ReturnsFalse_WhenEmailAlreadyExists()
    {
        var repository = new FakeUserRepository
        {
            EmailExists = true
        };

        var authService = new AuthService(
            repository,
            new PasswordHasher<User>());

        var request = new CreateUserRequestDto
        {
            FullName = "Another Technician",
            Email = "existing@test.com",
            Password = "technician123",
            Role = "Technician"
        };

        var result = await authService.CreateUserAsync(request);

        Assert.IsFalse(result);
        Assert.IsNull(repository.AddedUser);
    }

    [TestMethod]
    public async Task CreateUserAsync_ReturnsFalse_WhenRoleIsInvalid()
    {
        var repository = new FakeUserRepository();

        var authService = new AuthService(
            repository,
            new PasswordHasher<User>());

        var request = new CreateUserRequestDto
        {
            FullName = "Unexpected Role",
            Email = "unexpected@test.com",
            Password = "password123",
            Role = "SuperAdmin"
        };

        var result = await authService.CreateUserAsync(request);

        Assert.IsFalse(result);
        Assert.IsNull(repository.AddedUser);
    }

    [TestMethod]
    public async Task UpdateUser_LastActiveAdminCannotBeDemoted()
    {
        var repository = Account(UserRole.Admin);
        repository.ActiveAdminCount = 1;
        var service = new AuthService(repository, new PasswordHasher<User>());
        var result = await service.UpdateUserAsync(42, Edit(UserRole.Technician));
        Assert.AreEqual(UserUpdateResult.CannotDemoteLastActiveAdmin, result);
        Assert.AreEqual(UserRole.Admin, repository.UserToFindById!.Role);
        Assert.IsNull(repository.UpdatedUser);
    }

    [TestMethod]
    public async Task UpdateUser_AdminCanBeDemotedWhenAnotherActiveAdminRemains()
    {
        var repository = Account(UserRole.Admin);
        var service = new AuthService(repository, new PasswordHasher<User>());
        Assert.AreEqual(UserUpdateResult.Updated, await service.UpdateUserAsync(42, Edit(UserRole.Technician)));
        Assert.AreEqual(UserRole.Technician, repository.UpdatedUser!.Role);
    }

    [TestMethod]
    public async Task UpdateUser_LastAdminCanEditProfileWithoutChangingRole()
    {
        var repository = Account(UserRole.Admin);
        repository.ActiveAdminCount = 1;
        var service = new AuthService(repository, new PasswordHasher<User>());
        Assert.AreEqual(UserUpdateResult.Updated, await service.UpdateUserAsync(42, Edit(UserRole.Admin)));
    }

    [TestMethod]
    [DataRow("Technician")]
    [DataRow("Customer")]
    public async Task UpdateUser_RoleChangeBlockedWithOpenComplaints(string role)
    {
        var repository = Account(Enum.Parse<UserRole>(role));
        repository.HasOpenComplaints = true;
        var service = new AuthService(repository, new PasswordHasher<User>());
        Assert.AreEqual(UserUpdateResult.HasOpenComplaints, await service.UpdateUserAsync(42, Edit(UserRole.Admin)));
        Assert.IsNull(repository.UpdatedUser);
    }

    [TestMethod]
    [DataRow("Technician")]
    [DataRow("Customer")]
    public async Task DeactivateUser_BlockedWithOpenComplaints(string role)
    {
        var repository = Account(Enum.Parse<UserRole>(role));
        repository.HasOpenComplaints = true;
        var service = new AuthService(repository, new PasswordHasher<User>());
        Assert.AreEqual(UserStatusUpdateResult.HasOpenComplaints, await service.UpdateUserStatusAsync(99, 42, false));
        Assert.IsTrue(repository.UserToFindById!.IsActive);
        Assert.IsNull(repository.UpdatedUser);
    }

    [TestMethod]
    public async Task DeactivateUser_LastAdminRemainsProtected()
    {
        var repository = Account(UserRole.Admin);
        repository.ActiveAdminCount = 1;
        var service = new AuthService(repository, new PasswordHasher<User>());
        Assert.AreEqual(UserStatusUpdateResult.CannotDeactivateLastActiveAdmin,
            await service.UpdateUserStatusAsync(99, 42, false));
        Assert.IsNull(repository.UpdatedUser);
    }

    [TestMethod]
    public async Task DeactivateUser_NoOpenComplaintsSucceeds()
    {
        var repository = Account(UserRole.Technician);
        var service = new AuthService(repository, new PasswordHasher<User>());
        Assert.AreEqual(UserStatusUpdateResult.Updated, await service.UpdateUserStatusAsync(99, 42, false));
        Assert.IsFalse(repository.UpdatedUser!.IsActive);
    }

    private static FakeUserRepository Account(UserRole role) => new()
    {
        UserToFindById = new User { UserId = 42, FullName = "Test User", Email = "test@example.com", Role = role, IsActive = true }
    };

    private static UpdateUserRequestDto Edit(UserRole role) => new()
    {
        FullName = "Updated User", Email = "test@example.com", Role = role.ToString()
    };

    private sealed class FakeUserRepository : IUserRepository
    {
        public User? UserToFind { get; set; }

        public bool EmailExists { get; set; }

        public User? AddedUser { get; private set; }

        public User? UserToFindById { get; set; }

        public User? UpdatedUser { get; private set; }

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

        public Task<IReadOnlyList<User>> GetAllAsync()
        {
            return Task.FromResult<IReadOnlyList<User>>(
                Array.Empty<User>());
        }

        public Task<User?> GetByIdAsync(int userId)
        {
            var user = UserToFindById?.UserId == userId
                ? UserToFindById
                : null;

            return Task.FromResult(user);
        }

        public Task UpdateAsync(User user)
        {
            UpdatedUser = user;

            return Task.CompletedTask;
        }

        public int ActiveAdminCount { get; set; } = 2;

        public bool HasOpenComplaints { get; set; }

        public Task<bool> HasOpenComplaintsAsync(int userId, UserRole role)
            => Task.FromResult(HasOpenComplaints);

        public Task<T> ExecuteAccountChangeAsync<T>(Func<Task<T>> operation)
            => operation();

        public Task<bool> IsActiveInRoleAsync(int userId, UserRole role, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<int> CountActiveAdminsAsync()
        {
            return Task.FromResult(ActiveAdminCount);
        }
    }
}