
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RepairSystem.Application.Interfaces.Auth;
using RepairSystem.Application.Interfaces.Complaints;
using RepairSystem.Application.Interfaces.SystemLogs;
using System.Text;

namespace RepairSystem.Api.Tests;

public sealed class RepairApiFactory : WebApplicationFactory<Program>
{
    public const string Issuer = "RepairLifecycleSystem.Tests";
    public const string Audience = "RepairSystem.Api.Tests";

    public static readonly byte[] SigningKeyBytes =
        Encoding.UTF8.GetBytes("0123456789ABCDEF0123456789ABCDEF");

    public FakeAuthService FakeAuthService { get; } = new();

    public FakeComplaintService FakeComplaintService { get; } = new();

    public FakeSystemLogService FakeSystemLogService { get; } = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // These settings must be supplied before the API reads
        // its JWT configuration during Program.cs startup.
        builder.ConfigureHostConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:SigningKey"] =
                        Convert.ToBase64String(SigningKeyBytes),
                    ["Jwt:Issuer"] = Issuer,
                    ["Jwt:Audience"] = Audience,
                    ["Jwt:AccessTokenMinutes"] = "5",
                    ["ConnectionStrings:RepairDatabase"] =
                        @"Server=(localdb)\MSSQLLocalDB;Database=RepairLifecycleApiTests;Trusted_Connection=True;TrustServerCertificate=True"
                });
        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        // Prevent integration tests from writing to the real logging database.
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Serilog:SqlServer:Enabled"] = "false"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IAuthService>();
            services.AddSingleton<IAuthService>(FakeAuthService);

            services.RemoveAll<IComplaintService>();
            services.AddSingleton<IComplaintService>(FakeComplaintService);

            services.RemoveAll<ISystemLogService>();
            services.AddSingleton<ISystemLogService>(FakeSystemLogService);
        });
    }
}
