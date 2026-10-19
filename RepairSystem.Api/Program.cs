using Microsoft.EntityFrameworkCore;
using RepairSystem.Infrastructure.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using RepairSystem.Domain.Entities;
using RepairSystem.Application.Interfaces.Auth;
using RepairSystem.Application.Interfaces.Users;
using RepairSystem.Application.Services.Auth;
using RepairSystem.Infrastructure.Repositories.Users;

// start the API and load its configuration
var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
