
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using RepairSystem.Application.Contracts.Auth;

namespace RepairSystem.Api.Tests;

[TestClass]
public sealed class AuthControllerAuthorizationTests
{
    private const string Endpoint = "/api/Auth/users";

    [TestMethod]
    public async Task CreateUser_Returns401_WhenTokenIsMissing()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Endpoint,
            ValidRequest());

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(0, factory.FakeAuthService.CreateUserCallCount);
    }

    [TestMethod]
    public async Task CreateUser_Returns403_WhenCallerIsCustomer()
    {
        await AssertForbiddenForRoleAsync("Customer");
    }

    [TestMethod]
    public async Task CreateUser_Returns403_WhenCallerIsTechnician()
    {
        await AssertForbiddenForRoleAsync("Technician");
    }

    [TestMethod]
    public async Task CreateUser_Returns201_WhenCallerIsAdmin()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken("Admin"));

        var request = ValidRequest();

        var response = await client.PostAsJsonAsync(Endpoint, request);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        Assert.AreEqual(1, factory.FakeAuthService.CreateUserCallCount);
        Assert.IsNotNull(factory.FakeAuthService.LastCreatedRequest);
        Assert.AreEqual(
            request.Email,
            factory.FakeAuthService.LastCreatedRequest.Email);
    }

    private static async Task AssertForbiddenForRoleAsync(string role)
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(role));

        var response = await client.PostAsJsonAsync(
            Endpoint,
            ValidRequest());

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual(0, factory.FakeAuthService.CreateUserCallCount);
    }

    private static CreateUserRequestDto ValidRequest()
    {
        return new CreateUserRequestDto
        {
            FullName = "Integration Test User",
            Email = "integration-test@example.com",
            Password = "TestPassword123!",
            Role = "Technician"
        };
    }

    private static string CreateToken(string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "100"),
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
}
