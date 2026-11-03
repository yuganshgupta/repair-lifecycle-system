using Microsoft.IdentityModel.Tokens;
using RepairSystem.Application.Contracts.Auth;
using RepairSystem.Application.Contracts.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace RepairSystem.Api.Tests;

[TestClass]
public sealed class AdminUsersControllerTests
{
    private const string Endpoint = "/api/Auth/users";

    [TestMethod]
    public async Task GetUsers_Returns401_WhenTokenIsMissing()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(0, factory.FakeAuthService.GetUsersCallCount);
    }

    [TestMethod]
    public async Task GetUsers_Returns403_WhenCallerIsCustomer()
    {
        await AssertForbiddenForRoleAsync("Customer");
    }

    [TestMethod]
    public async Task GetUsers_Returns403_WhenCallerIsTechnician()
    {
        await AssertForbiddenForRoleAsync("Technician");
    }

    [TestMethod]
    public async Task GetUsers_Returns200_WithUserList_WhenCallerIsAdmin()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, "Admin");

        factory.FakeAuthService.Users =
        [
            new AdminUserDto
            {
                UserId = 1,
                FullName = "Demo Customer",
                Email = "customer@example.com",
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new AdminUserDto
            {
                UserId = 2,
                FullName = "Demo Technician",
                Email = "technician@example.com",
                Role = "Technician",
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            }
        ];

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<
            List<AdminUserDto>>();

        Assert.IsNotNull(users);
        Assert.HasCount(2, users);

        Assert.AreEqual("Demo Customer", users[0].FullName);
        Assert.AreEqual("Customer", users[0].Role);

        Assert.AreEqual("Demo Technician", users[1].FullName);
        Assert.IsFalse(users[1].IsActive);

        Assert.AreEqual(1, factory.FakeAuthService.GetUsersCallCount);

        var responseJson = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("PasswordHash", responseJson);
        Assert.DoesNotContain("passwordHash", responseJson);
    }

    [TestMethod]
    public async Task GetUsers_Returns200_WhenNoUsersExist()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, "Admin");

        factory.FakeAuthService.Users =
            Array.Empty<AdminUserDto>();

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<
            List<AdminUserDto>>();

        Assert.IsNotNull(users);
        Assert.IsEmpty(users);
    }

    private static async Task AssertForbiddenForRoleAsync(string role)
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, role);

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

        Assert.AreEqual(0, factory.FakeAuthService.GetUsersCallCount);
    }

    private static HttpClient CreateAuthenticatedClient(
        RepairApiFactory factory,
        string role)
    {
        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateToken(role));

        return client;
    }

    private static string CreateToken(string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Name, "Integration Test User"),
            new Claim(ClaimTypes.Email, "integration-test@example.com"),
            new Claim(ClaimTypes.Role, role)
        };

        var signingKey = new SymmetricSecurityKey(
            RepairApiFactory.SigningKeyBytes);

        var token = new JwtSecurityToken(
            issuer: RepairApiFactory.Issuer,
            audience: RepairApiFactory.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [TestMethod]
    public async Task UpdateUser_Returns401_WhenTokenIsMissing()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/42",
            ValidUpdateRequest());

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(0, factory.FakeAuthService.UpdateUserCallCount);
    }

    [TestMethod]
    public async Task UpdateUser_Returns403_WhenCallerIsCustomer()
    {
        await AssertUpdateForbiddenForRoleAsync("Customer");
    }

    [TestMethod]
    public async Task UpdateUser_Returns403_WhenCallerIsTechnician()
    {
        await AssertUpdateForbiddenForRoleAsync("Technician");
    }

    [TestMethod]
    public async Task UpdateUser_Returns204_WhenUpdateSucceeds()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, "Admin");

        var request = new UpdateUserRequestDto
        {
            FullName = "Updated Customer",
            Email = "updated@example.com",
            Role = "Technician"
        };

        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/42",
            request);

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        Assert.AreEqual(1, factory.FakeAuthService.UpdateUserCallCount);
        Assert.AreEqual(42, factory.FakeAuthService.LastUpdatedUserId);

        Assert.IsNotNull(factory.FakeAuthService.LastUpdateRequest);
        Assert.AreEqual(
            "Updated Customer",
            factory.FakeAuthService.LastUpdateRequest.FullName);
    }

    [TestMethod]
    public async Task UpdateUser_Returns404_WhenUserDoesNotExist()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, "Admin");

        factory.FakeAuthService.UpdateResult =
            UserUpdateResult.NotFound;

        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/999",
            ValidUpdateRequest());

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateUser_Returns409_WhenEmailAlreadyExists()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, "Admin");

        factory.FakeAuthService.UpdateResult =
            UserUpdateResult.EmailAlreadyExists;

        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/42",
            ValidUpdateRequest());

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task UpdateUser_Returns400_WhenRequestIsInvalid()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, "Admin");

        var invalidRequest = new UpdateUserRequestDto
        {
            FullName = "",
            Email = "not-an-email",
            Role = "SuperAdmin"
        };

        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/42",
            invalidRequest);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        Assert.AreEqual(0, factory.FakeAuthService.UpdateUserCallCount);
    }

    private static async Task AssertUpdateForbiddenForRoleAsync(string role)
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, role);

        var response = await client.PutAsJsonAsync(
            $"{Endpoint}/42",
            ValidUpdateRequest());

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

        Assert.AreEqual(0, factory.FakeAuthService.UpdateUserCallCount);
    }

    private static UpdateUserRequestDto ValidUpdateRequest()
    {
        return new UpdateUserRequestDto
        {
            FullName = "Updated Customer",
            Email = "updated@example.com",
            Role = "Customer"
        };
    }
}