
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using RepairSystem.Web.Components;
using RepairSystem.Web.Services.Authentication;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<
    IApiTokenSessionStore,
    MemoryApiTokenSessionStore>();

builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RepairSystem.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;

        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(
            WebAuthenticationDefaults.SessionLifetimeHours);
        options.SlidingExpiration = false;

        // Reject a cookie if its associated server-side session
        // no longer exists or has expired.
        options.Events.OnValidatePrincipal = async context =>
        {
            var store = context.HttpContext.RequestServices
                .GetRequiredService<IApiTokenSessionStore>();

            var sessionId = context.Principal?
                .FindFirst(WebAuthenticationDefaults.SessionIdClaimType)
                ?.Value;

            if (string.IsNullOrWhiteSpace(sessionId) ||
                !store.TryGet(sessionId, out _))
            {
                context.RejectPrincipal();

                await context.HttpContext.SignOutAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

// The Web app calls the API over HTTP from the server.
builder.Services.AddHttpClient("RepairApi", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ApiBaseUrl"]
        ?? "http://localhost:5276/");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute(
    "/not-found",
    createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
