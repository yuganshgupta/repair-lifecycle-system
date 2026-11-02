using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using RepairSystem.Application.Contracts.Complaints;

namespace RepairSystem.Api.Tests;

[TestClass]
public sealed class CustomerComplaintReadTests
{
    private const string Endpoint = "/api/Complaints";

    [TestMethod]
    public async Task GetMyComplaints_Returns401_WhenTokenIsMissing()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient(); // Fixed: unauthenticated client (no token)

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(0, factory.FakeComplaintService.CallCount);
    }

    [TestMethod]
    public async Task GetMyComplaints_Returns403_WhenCallerIsTechnician()
    {
        await AssertForbiddenForRoleAsync("Technician");
    }

    [TestMethod]
    public async Task GetMyComplaints_Returns403_WhenCallerIsAdmin()
    {
        await AssertForbiddenForRoleAsync("Admin");
    }

    [TestMethod]
    public async Task GetMyComplaints_Returns200_WithCustomerComplaints()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateCustomerClient(factory); // Fixed: passed factory

        factory.FakeComplaintService.Complaints =
        [
            new CustomerComplaintDto
            {
                ComplaintId = 1,
                TicketNumber = "TKT-20261030-TEST0001",
                DeviceType = "Laptop",
                DeviceBrand = "Dell",
                DeviceModel = "Inspiron 15",
                IssueDescription = "Laptop shuts down unexpectedly.",
                Status = "Submitted",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        ];

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var complaints =
            await response.Content.ReadFromJsonAsync<List<CustomerComplaintDto>>();

        Assert.IsNotNull(complaints);
        Assert.HasCount(1, complaints); // Fixed: MSTest0037 analyzer
        Assert.AreEqual("TKT-20261030-TEST0001", complaints[0].TicketNumber);
        Assert.AreEqual("Submitted", complaints[0].Status);

        Assert.AreEqual(1, factory.FakeComplaintService.CallCount);
        Assert.AreEqual(100, factory.FakeComplaintService.LastRequestedCustomerId);
    }

    [TestMethod]
    public async Task GetMyComplaints_UsesCustomerIdFromToken_NotQueryString()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateCustomerClient(factory); // Fixed: passed factory

        var response = await client.GetAsync($"{Endpoint}?customerId=999");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(100, factory.FakeComplaintService.LastRequestedCustomerId);
    }

    [TestMethod]
    public async Task GetMyComplaints_Returns200_WhenCustomerHasNoComplaints()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateCustomerClient(factory); // Fixed: passed factory

        factory.FakeComplaintService.Complaints =
            Array.Empty<CustomerComplaintDto>();

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var complaints =
            await response.Content.ReadFromJsonAsync<List<CustomerComplaintDto>>();

        Assert.IsNotNull(complaints);
        Assert.IsEmpty(complaints); // Fixed: MSTest0037 analyzer
    }

    [TestMethod]
    public async Task GetMyComplaints_Returns401_WhenUserIdClaimIsInvalid()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateToken("Customer", "not-an-integer"));

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(0, factory.FakeComplaintService.CallCount);
    }

    private static async Task AssertForbiddenForRoleAsync(string role)
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateToken(role, "100"));

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual(0, factory.FakeComplaintService.CallCount);
    }

    private static HttpClient CreateCustomerClient(RepairApiFactory factory)
    {
        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateToken("Customer", "100"));

        return client;
    }

    private static string CreateToken(string role, string userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
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