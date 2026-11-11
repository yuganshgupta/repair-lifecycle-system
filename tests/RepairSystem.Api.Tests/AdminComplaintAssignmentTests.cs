
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using RepairSystem.Application.Contracts.Complaints;

namespace RepairSystem.Api.Tests;

[TestClass]
public sealed class AdminComplaintAssignmentTests
{
    private const string Endpoint =
        "/api/admin/complaints/101/assign";

    [TestMethod]
    public async Task AssignTechnician_Returns401_WhenTokenIsMissing()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            Endpoint,
            new AssignTechnicianRequestDto { TechnicianId = 20 });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(0, factory.FakeComplaintService.AssignmentCallCount);
    }

    [TestMethod]
    public async Task AssignTechnician_Returns403_WhenCallerIsCustomer()
    {
        await AssertForbiddenForRoleAsync("Customer");
    }

    [TestMethod]
    public async Task AssignTechnician_Returns403_WhenCallerIsTechnician()
    {
        await AssertForbiddenForRoleAsync("Technician");
    }

    [TestMethod]
    public async Task AssignTechnician_Returns204_WhenCallerIsAdmin()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(
            factory, "Admin", "5");

        var response = await client.PostAsJsonAsync(
            Endpoint,
            new AssignTechnicianRequestDto { TechnicianId = 20 });

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        Assert.AreEqual(
            1,
            factory.FakeComplaintService.AssignmentCallCount);

        Assert.AreEqual(
            101,
            factory.FakeComplaintService.LastAssignedComplaintId);

        Assert.AreEqual(
            20,
            factory.FakeComplaintService.LastAssignedTechnicianId);

        // The Admin ID must come from the JWT.
        Assert.AreEqual(
            5,
            factory.FakeComplaintService.LastAssigningAdminId);
    }

    [TestMethod]
    public async Task AssignTechnician_Returns401_WhenAdminIdClaimIsInvalid()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(
            factory, "Admin", "invalid-id");

        var response = await client.PostAsJsonAsync(
            Endpoint,
            new AssignTechnicianRequestDto { TechnicianId = 20 });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        Assert.AreEqual(
            0,
            factory.FakeComplaintService.AssignmentCallCount);
    }

    [TestMethod]
    public async Task AssignTechnician_Returns400_WhenTechnicianIdIsInvalid()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(
            factory, "Admin", "5");

        var response = await client.PostAsJsonAsync(
            Endpoint,
            new AssignTechnicianRequestDto { TechnicianId = 0 });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        // Model validation should reject the request before
        // the Application service is called.
        Assert.AreEqual(
            0,
            factory.FakeComplaintService.AssignmentCallCount);
    }

    [TestMethod]
    [DataRow(
        ComplaintAssignmentResult.ComplaintNotFound,
        HttpStatusCode.NotFound)]
    [DataRow(
        ComplaintAssignmentResult.TechnicianNotFound,
        HttpStatusCode.NotFound)]
    [DataRow(
        ComplaintAssignmentResult.TechnicianInactive,
        HttpStatusCode.Conflict)]
    [DataRow(
        ComplaintAssignmentResult.UserIsNotTechnician,
        HttpStatusCode.BadRequest)]
    [DataRow(
        ComplaintAssignmentResult.AlreadyAssigned,
        HttpStatusCode.Conflict)]
    [DataRow(
        ComplaintAssignmentResult.InvalidComplaintStatus,
        HttpStatusCode.Conflict)]
    public async Task AssignTechnician_ReturnsExpectedStatus_WhenAssignmentFails(
        ComplaintAssignmentResult assignmentResult,
        HttpStatusCode expectedStatus)
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(
            factory, "Admin", "5");

        factory.FakeComplaintService.AssignmentResult =
            assignmentResult;

        var response = await client.PostAsJsonAsync(
            Endpoint,
            new AssignTechnicianRequestDto { TechnicianId = 20 });

        Assert.AreEqual(expectedStatus, response.StatusCode);

        Assert.AreEqual(
            1,
            factory.FakeComplaintService.AssignmentCallCount);
    }

    private static async Task AssertForbiddenForRoleAsync(string role)
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(
            factory, role, "200");

        var response = await client.PostAsJsonAsync(
            Endpoint,
            new AssignTechnicianRequestDto { TechnicianId = 20 });

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

        Assert.AreEqual(
            0,
            factory.FakeComplaintService.AssignmentCallCount);
    }

    private static HttpClient CreateAuthenticatedClient(
        RepairApiFactory factory,
        string role,
        string userId)
    {
        var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                CreateToken(role, userId));

        return client;
    }

    private static string CreateToken(string role, string userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, "Integration Test User"),
            new Claim(
                ClaimTypes.Email,
                "integration-test@example.com"),
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
