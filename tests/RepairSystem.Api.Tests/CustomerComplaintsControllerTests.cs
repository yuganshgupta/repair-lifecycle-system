using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using RepairSystem.Application.Contracts.Complaints;

namespace RepairSystem.Api.Tests;

[TestClass]
public sealed class CustomerComplaintCreationTests
{
    private const string Endpoint = "/api/Complaints";

    [TestMethod]
    public async Task CreateComplaint_Returns401_WhenTokenIsMissing()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Endpoint,
            ValidRequest());

        Assert.AreEqual(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.AreEqual(
            0,
            factory.FakeComplaintService.CreateCallCount);
    }

    [TestMethod]
    public async Task CreateComplaint_Returns403_WhenCallerIsTechnician()
    {
        await AssertForbiddenForRoleAsync("Technician");
    }

    [TestMethod]
    public async Task CreateComplaint_Returns403_WhenCallerIsAdmin()
    {
        await AssertForbiddenForRoleAsync("Admin");
    }

    [TestMethod]
    public async Task CreateComplaint_Returns201_WhenCallerIsCustomer()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateToken("Customer", "100"));

        var response = await client.PostAsJsonAsync(
            Endpoint,
            ValidRequest());

        Assert.AreEqual(
            HttpStatusCode.Created,
            response.StatusCode);

        var createdComplaint =
            await response.Content.ReadFromJsonAsync<CustomerComplaintDto>();

        Assert.IsNotNull(createdComplaint);

        Assert.AreEqual(
            "TKT-20261030-TEST0001",
            createdComplaint.TicketNumber);

        Assert.AreEqual(
            "Submitted",
            createdComplaint.Status);

        Assert.AreEqual(
            1,
            factory.FakeComplaintService.CreateCallCount);

        Assert.AreEqual(
            100,
            factory.FakeComplaintService.LastCreatedCustomerId);

        Assert.IsNotNull(factory.FakeComplaintService.LastCreateRequest);

        Assert.AreEqual(
            "Dell",
            factory.FakeComplaintService.LastCreateRequest.DeviceBrand);
    }

    [TestMethod]
    public async Task CreateComplaint_UsesCustomerIdFromToken_NotRequestBody()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateToken("Customer", "100"));

        // Deliberately include a customerId that does not belong
        // to the authenticated user. The request DTO has no such property.
        var request = new
        {
            customerId = 999,
            deviceType = "Laptop",
            deviceBrand = "Dell",
            deviceModel = "Inspiron 15",
            serialNumber = "SN123456",
            issueDescription = "The laptop shuts down unexpectedly.",
            claimedWarranty = true
        };

        var response = await client.PostAsJsonAsync(
            Endpoint,
            request);

        Assert.AreEqual(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.AreEqual(
            100,
            factory.FakeComplaintService.LastCreatedCustomerId);
    }

    [TestMethod]
    public async Task CreateComplaint_Returns400_WhenRequestIsInvalid()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateToken("Customer", "100"));

        var invalidRequest = new CreateComplaintRequestDto
        {
            DeviceType = "",
            DeviceBrand = "",
            DeviceModel = "",
            IssueDescription = "Short"
        };

        var response = await client.PostAsJsonAsync(
            Endpoint,
            invalidRequest);

        Assert.AreEqual(
            HttpStatusCode.BadRequest,
            response.StatusCode);

        Assert.AreEqual(
            0,
            factory.FakeComplaintService.CreateCallCount);
    }

    private static async Task AssertForbiddenForRoleAsync(string role)
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateToken(role, "100"));

        var response = await client.PostAsJsonAsync(
            Endpoint,
            ValidRequest());

        Assert.AreEqual(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.AreEqual(
            0,
            factory.FakeComplaintService.CreateCallCount);
    }

    private static CreateComplaintRequestDto ValidRequest()
    {
        return new CreateComplaintRequestDto
        {
            DeviceType = "Laptop",
            DeviceBrand = "Dell",
            DeviceModel = "Inspiron 15",
            SerialNumber = "SN123456",
            IssueDescription = "The laptop shuts down unexpectedly.",
            ClaimedWarranty = true
        };
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