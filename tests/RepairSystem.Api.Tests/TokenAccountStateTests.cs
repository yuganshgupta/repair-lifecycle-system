using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using RepairSystem.Domain.Enums;

namespace RepairSystem.Api.Tests;

[TestClass]
public sealed class TokenAccountStateTests
{
    [TestMethod]
    [DataRow("inactive")]
    [DataRow("demoted")]
    [DataRow("deleted")]
    public async Task PreviouslyValidAdminToken_IsRejectedAfterAccountChanges(string change)
    {
        using var factory = new RepairApiFactory();
        factory.TokenAccounts.AllowSyntheticAccounts = false;
        factory.TokenAccounts.Accounts[100] = (true, UserRole.Admin);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token());

        using var before = await client.GetAsync("/api/Auth/users");
        Assert.AreEqual(HttpStatusCode.OK, before.StatusCode);

        switch (change)
        {
            case "inactive": factory.TokenAccounts.Accounts[100] = (false, UserRole.Admin); break;
            case "demoted": factory.TokenAccounts.Accounts[100] = (true, UserRole.Technician); break;
            case "deleted": factory.TokenAccounts.Accounts.Remove(100); break;
        }

        using var after = await client.GetAsync("/api/Auth/users");
        Assert.AreEqual(HttpStatusCode.Unauthorized, after.StatusCode);
        Assert.AreEqual(1, factory.FakeAuthService.GetUsersCallCount);
    }

    [TestMethod]
    public async Task InactiveTechnicianToken_CannotUpdateComplaintStatus()
    {
        using var factory = new RepairApiFactory();
        factory.TokenAccounts.AllowSyntheticAccounts = false;
        factory.TokenAccounts.Accounts[100] = (false, UserRole.Technician);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Technician"));
        using var request = new HttpRequestMessage(HttpMethod.Patch, "/api/technician/complaints/101/status")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { newStatus = 2 })
        };
        using var response = await client.SendAsync(request);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string Token(string role = "Admin") => new JwtSecurityTokenHandler().WriteToken(
        new JwtSecurityToken(RepairApiFactory.Issuer, RepairApiFactory.Audience,
            new[] { new Claim(ClaimTypes.NameIdentifier, "100"), new Claim(ClaimTypes.Role, role) },
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(RepairApiFactory.SigningKeyBytes),
                SecurityAlgorithms.HmacSha256)));
}
