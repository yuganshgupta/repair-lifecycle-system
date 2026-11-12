using Microsoft.IdentityModel.Tokens;
using RepairSystem.Application.Contracts.Complaints;
using RepairSystem.Domain.Enums;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace RepairSystem.Api.Tests;

[TestClass]
public sealed class TechnicianComplaintTests
{
    private const string Endpoint = "/api/technician/complaints";

    [TestMethod]
    public async Task GetAssignedComplaints_Returns401_WhenTokenIsMissing()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(0, factory.FakeComplaintService.TechnicianCallCount);
    }

    [TestMethod]
    public async Task GetAssignedComplaints_Returns403_WhenCallerIsCustomer()
    {
        await AssertForbiddenForRoleAsync("Customer");
    }

    [TestMethod]
    public async Task GetAssignedComplaints_Returns403_WhenCallerIsAdmin()
    {
        await AssertForbiddenForRoleAsync("Admin");
    }

    [TestMethod]
    public async Task GetAssignedComplaints_Returns200_WhenCallerIsTechnician()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, "Technician", "200");

        factory.FakeComplaintService.TechnicianComplaints =
        [
            new TechnicianComplaintDto
            {
                ComplaintId = 1,
                TicketNumber = "TKT-20261102-TEST0001",
                CustomerName = "Demo Customer",
                DeviceType = "Laptop",
                DeviceBrand = "Dell",
                DeviceModel = "Inspiron 15",
                IssueDescription = "Laptop overheats during use.",
                ClaimedWarranty = true,
                Status = "Assigned",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        ];

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var complaints =
            await response.Content.ReadFromJsonAsync<
                List<TechnicianComplaintDto>>();

        Assert.IsNotNull(complaints);
        Assert.HasCount(1, complaints);
        Assert.AreEqual("Demo Customer", complaints[0].CustomerName);
        Assert.AreEqual("Assigned", complaints[0].Status);

        Assert.AreEqual(
            1,
            factory.FakeComplaintService.TechnicianCallCount);

        Assert.AreEqual(
            200,
            factory.FakeComplaintService.LastRequestedTechnicianId);
    }

    [TestMethod]
    public async Task GetAssignedComplaints_UsesTechnicianIdFromToken_NotQueryString()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, "Technician", "200");

        var response = await client.GetAsync(
            $"{Endpoint}?technicianId=999");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        Assert.AreEqual(
            200,
            factory.FakeComplaintService.LastRequestedTechnicianId);
    }

    [TestMethod]
    public async Task GetAssignedComplaints_Returns200_WhenNoComplaintsAreAssigned()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, "Technician", "200");

        factory.FakeComplaintService.TechnicianComplaints =
            Array.Empty<TechnicianComplaintDto>();

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var complaints =
            await response.Content.ReadFromJsonAsync<
                List<TechnicianComplaintDto>>();

        Assert.IsNotNull(complaints);
        Assert.IsEmpty(complaints);
    }

    [TestMethod]
    public async Task GetAssignedComplaints_Returns401_WhenUserIdClaimIsInvalid()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(
            factory,
            "Technician",
            "invalid-id");

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        Assert.AreEqual(
            0,
            factory.FakeComplaintService.TechnicianCallCount);
    }

    private static async Task AssertForbiddenForRoleAsync(string role)
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(factory, role, "200");

        var response = await client.GetAsync(Endpoint);

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);

        Assert.AreEqual(
            0,
            factory.FakeComplaintService.TechnicianCallCount);
    }


    [TestMethod]
    public async Task UpdateComplaintStatus_Returns401_WhenTokenIsMissing()
    {
        using var factory = new RepairApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PatchAsJsonAsync(
            $"{Endpoint}/101/status",
            new UpdateTechnicianComplaintStatusRequestDto
            {
                NewStatus = ComplaintStatus.InDiagnosis
            });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.AreEqual(0, factory.FakeComplaintService.StatusUpdateCallCount);
    }

    [TestMethod]
    public async Task UpdateComplaintStatus_Returns403_WhenCallerIsCustomer()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(
            factory, "Customer", "200");

        var response = await client.PatchAsJsonAsync(
            $"{Endpoint}/101/status",
            new UpdateTechnicianComplaintStatusRequestDto
            {
                NewStatus = ComplaintStatus.InDiagnosis
            });

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual(0, factory.FakeComplaintService.StatusUpdateCallCount);
    }

    [TestMethod]
    public async Task UpdateComplaintStatus_Returns204_WhenTechnicianUpdatesOwnComplaint()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(
            factory, "Technician", "200");

        var response = await client.PatchAsJsonAsync(
            $"{Endpoint}/101/status",
            new UpdateTechnicianComplaintStatusRequestDto
            {
                NewStatus = ComplaintStatus.InDiagnosis
            });

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

        Assert.AreEqual(1, factory.FakeComplaintService.StatusUpdateCallCount);
        Assert.AreEqual(101,
            factory.FakeComplaintService.LastStatusUpdateComplaintId);
        Assert.AreEqual(200,
            factory.FakeComplaintService.LastStatusUpdateTechnicianId);
        Assert.AreEqual(
            ComplaintStatus.InDiagnosis,
            factory.FakeComplaintService.LastRequestedStatus);
    }

    [TestMethod]
    public async Task UpdateComplaintStatus_Returns409_WhenTransitionIsInvalid()
    {
        using var factory = new RepairApiFactory();
        using var client = CreateAuthenticatedClient(
            factory, "Technician", "200");

        factory.FakeComplaintService.StatusUpdateResult =
            TechnicianStatusUpdateResult.InvalidTransition;

        var response = await client.PatchAsJsonAsync(
            $"{Endpoint}/101/status",
            new UpdateTechnicianComplaintStatusRequestDto
            {
                NewStatus = ComplaintStatus.Completed
            });

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
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