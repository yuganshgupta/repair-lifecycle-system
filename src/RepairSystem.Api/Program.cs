using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RepairSystem.Api.OpenApi;
using RepairSystem.Api.Security;
using RepairSystem.Application.Interfaces.Auth;
using RepairSystem.Application.Interfaces.Complaints;
using RepairSystem.Application.Interfaces.Users;
using RepairSystem.Application.Services.Auth;
using RepairSystem.Application.Services.Complaints;
using RepairSystem.Domain.Entities;
using RepairSystem.Infrastructure.Data;
using RepairSystem.Infrastructure.Repositories.Complaints;
using RepairSystem.Infrastructure.Repositories.Users;
using Serilog;
using Serilog.Events;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using Serilog.Sinks.MSSqlServer;
using RepairSystem.Application.Interfaces.SystemLogs;
using RepairSystem.Application.Services.SystemLogs;
using RepairSystem.Infrastructure.Repositories.SystemLogs;


// start the API and load its configuration
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for API diagnostic logging.
var logDirectory = Path.Combine(
    builder.Environment.ContentRootPath,
    "logs");

Directory.CreateDirectory(logDirectory);

builder.Services.AddSerilog((services, loggerConfiguration) =>
{
    loggerConfiguration
        .MinimumLevel.Information()
            .MinimumLevel.Override(
                "Microsoft.AspNetCore",
                LogEventLevel.Warning)
           .MinimumLevel.Override(
                "Microsoft.EntityFrameworkCore",
                 LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithProperty(
            "Application",
            "RepairSystem.Api")
        .WriteTo.Console()
        .WriteTo.File(
            Path.Combine(logDirectory, "repair-api-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7);

    // SQL logging is explicitly controlled by configuration.
    var sqlLoggingEnabled = builder.Configuration.GetValue<bool>(
        "Serilog:SqlServer:Enabled");

    // Integration tests set this to false.
    if (!sqlLoggingEnabled)
    {
        return;
    }

    var sqlConnectionString = builder.Configuration
        .GetConnectionString("RepairDatabase");

    if (string.IsNullOrWhiteSpace(sqlConnectionString))
    {
        throw new InvalidOperationException(
            "SQL logging is enabled, but the RepairDatabase connection string is missing.");
    }

    var columnOptions = new ColumnOptions
    {
        AdditionalColumns = new Collection<SqlColumn>
        {
            new()
            {
                ColumnName = "ActorUserId",
                DataType = SqlDbType.Int,
                AllowNull = true,
                NonClusteredIndex = true
            },
            new()
            {
                ColumnName = "ActorRole",
                DataType = SqlDbType.NVarChar,
                DataLength = 32,
                AllowNull = true
            },
            new()
            {
                ColumnName = "RequestMethod",
                DataType = SqlDbType.NVarChar,
                DataLength = 16,
                AllowNull = true
            },
            new()
            {
                ColumnName = "RequestPath",
                DataType = SqlDbType.NVarChar,
                DataLength = 512,
                AllowNull = true
            },
            new()
            {
                ColumnName = "StatusCode",
                DataType = SqlDbType.Int,
                AllowNull = true
            },
            new()
            {
                ColumnName = "Elapsed",
                DataType = SqlDbType.Float,
                AllowNull = true
            },
            new()
            {
                ColumnName = "CorrelationId",
                DataType = SqlDbType.NVarChar,
                DataLength = 64,
                AllowNull = true
            },
            new()
            {
                ColumnName = "Action",
                DataType = SqlDbType.NVarChar,
                DataLength = 64,
                AllowNull = true
            },
            new()
            {
                ColumnName = "ResourceType",
                DataType = SqlDbType.NVarChar,
                DataLength = 64,
                AllowNull = true
            },
            new()
            {
                ColumnName = "ResourceId",
                DataType = SqlDbType.Int,
                AllowNull = true
            },
            new()
            {
                ColumnName = "Outcome",
                DataType = SqlDbType.NVarChar,
                DataLength = 32,
                AllowNull = true
            },
            new()
            {
                ColumnName = "Application",
                DataType = SqlDbType.NVarChar,
                DataLength = 64,
                AllowNull = true
            }
        }
    };

    // Store timestamps in UTC and optimize recent-log queries.
    columnOptions.TimeStamp.ConvertToUtc = true;
    columnOptions.TimeStamp.NonClusteredIndex = true;
    columnOptions.TimeStamp.NonClusteredIndexDirection =
        SqlIndexDirection.Desc;

    var sinkOptions = new MSSqlServerSinkOptions
    {
        TableName = "SystemLogs",
        SchemaName = "dbo",
        AutoCreateSqlTable = false,
        BatchPostingLimit = 50,
        BatchPeriod = TimeSpan.FromSeconds(5)
    };

    loggerConfiguration.WriteTo.MSSqlServer(
        connectionString: sqlConnectionString,
        sinkOptions: sinkOptions,
        columnOptions: columnOptions,
        restrictedToMinimumLevel: LogEventLevel.Information);
});


// get the JWT key from User Secrets
// we don't want to store the actual key in appsettings.json
var jwtKeyBase64 = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "JWT signing key was not found.");

// convert the key from a Base64 string back into its original bytes
var signingKeyBytes = Convert.FromBase64String(jwtKeyBase64);

// wrap the bytes in the security key object that the JWT library expects
var signingKey = new SymmetricSecurityKey(signingKeyBytes);

// get the issuer value that our tokens should contain
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer was not found.");

// get the audience value so we can check that tokens are meant for our API
var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience was not found.");

// get the token lifetime from appsettings.json
var accessTokenMinutesText = builder.Configuration["Jwt:AccessTokenMinutes"]
    ?? throw new InvalidOperationException(
        "JWT access token lifetime was not found.");

// make sure the lifetime is a valid positive number
if (!int.TryParse(accessTokenMinutesText, out var accessTokenMinutes) ||
    accessTokenMinutes <= 0)
{
    throw new InvalidOperationException(
        "JWT access token lifetime must be a positive integer.");
}

// get the connection string for our LocalDB database
var connectionString = builder.Configuration
    .GetConnectionString("RepairDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'RepairDatabase' was not found.");

// configure EF Core to use SQL Server and register the context
// so the API can request RepairDbContext through dependency injection
builder.Services.AddDbContext<RepairDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register the services needed for user registration
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddScoped<
    IComplaintRepository,
    ComplaintRepository>();

builder.Services.AddScoped<
    IComplaintService,
    ComplaintService>();

builder.Services.AddScoped<
    ISystemLogRepository,
    SystemLogRepository>();

builder.Services.AddScoped<
    ISystemLogService,
    SystemLogService>();

builder.Services.AddSingleton<
    IAccessTokenGenerator,
    JwtAccessTokenGenerator>();


// These are the checks the API uses before trusting a JWT
builder.Services.AddAuthentication(
    JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true

    });

builder.Services.AddAuthorization();


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<JwtBearerOpenApiTransformer>();
    options.AddOperationTransformer<JwtBearerOpenApiTransformer>();
});

var app = builder.Build();

app.UseSerilogRequestLogging(options =>
{
    // Categorise failed requests appropriately.
    options.GetLevel = (httpContext, elapsed, exception) =>
    {
        if (exception is not null ||
            httpContext.Response.StatusCode >= 500)
        {
            return LogEventLevel.Error;
        }

        if (httpContext.Response.StatusCode >= 400)
        {
            return LogEventLevel.Warning;
        }

        return LogEventLevel.Information;
    };

    // Attach authenticated user and request information.
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        var userId = httpContext.User
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (int.TryParse(userId, out var actorUserId))
        {
            diagnosticContext.Set("ActorUserId", actorUserId);
        }

        var role = httpContext.User
            .FindFirst(ClaimTypes.Role)?.Value;

        if (!string.IsNullOrWhiteSpace(role))
        {
            diagnosticContext.Set("ActorRole", role);
        }

        var correlationId =
            Activity.Current?.TraceId.ToString()
            ?? httpContext.TraceIdentifier;

        diagnosticContext.Set("CorrelationId", correlationId);
    };
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "RepairSystem API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}