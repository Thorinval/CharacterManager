using CharacterManager.Server.Data;
using CharacterManager.Server.Services;
using CharacterManager.Server;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using CharacterManager.Components;
using Serilog;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from appsettings
builder.Host.UseSerilog((ctx, services, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Authentication & Authorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Configure SQLite database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlite("Data Source=charactermanager.db");
    
    // Enable detailed logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
    
    // Log SQL queries with caller information
    options.LogTo(
        message => Log.Debug("[EF Core] {Message}", message),
        new[] { Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandExecuting }
    );
});

// Register ProfileService BEFORE PersonnageService (dependency order)
builder.Services.AddApplicationServices(builder.Environment);

// API controllers (ex: ResourcesController)
builder.Services.AddControllers();

var app = builder.Build();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbInitService = scope.ServiceProvider.GetRequiredService<DatabaseInitializationService>();

    await dbInitService.InitializeDatabaseAsync();
    await dbInitService.InitializeAppSettingsAndCheckStateAsync();
}

// Security pipeline
app.UseAuthentication();
app.UseAuthorization();

// Login endpoint for handling authentication (avoids Blazor Server SignalR conflict)
app.MapPost("/api/login", HandleLoginAsync);

// Logout endpoint to avoid SignalR response conflicts
app.MapGet("/api/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/login");
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}


app.UseAntiforgery();

// Serve static files from wwwroot (icons, images, css)
app.UseStaticFiles();

// API endpoints
app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

// Helper method to generate secure random password
static string GenerateSecurePassword()
{
    var randomGenerator = RandomNumberGenerator.Create();
    byte[] data = new byte[16];
    randomGenerator.GetBytes(data);

    // Shuffle the password to avoid predictable pattern
    return BitConverter.ToString(data);
}

/// <summary>
/// Handles the login POST request with profile authentication and validation
/// </summary>
async Task HandleLoginAsync(HttpContext context, ProfileService profileService)
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    // Validate input
    if (!ValidateLoginInput(username, password, context))
        return;

    // Bootstrap default admin if no profiles exist
    await EnsureDefaultAdminExistsAsync(profileService);

    // Authenticate the user
    var profile = await AuthenticateProfileAsync(username, password, profileService, context);
    if (profile == null)
        return;

    // Sign in the user
    await SignInProfileAsync(username, profile, context);
}

/// <summary>
/// Validates login input parameters
/// </summary>
bool ValidateLoginInput(string? username, string? password, HttpContext context)
{
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        context.Response.Redirect("/login?error=required");
        return false;
    }
    return true;
}

/// <summary>
/// Ensures a default admin account exists, creating one if needed
/// </summary>
async Task EnsureDefaultAdminExistsAsync(ProfileService profileService)
{
    var allProfiles = await profileService.GetAllAsync();
    if (allProfiles != null && allProfiles.Count > 0)
        return;

    var randomPassword = GenerateSecurePassword();
    await profileService.CreateUserAsync("admin", randomPassword, "admin");
    
    Console.WriteLine("\n" + new string('=', 80));
    Console.WriteLine("[SECURITY] No profiles found - created default admin account");
    Console.WriteLine("[SECURITY] Username: admin");
    Console.WriteLine($"[SECURITY] Password: {randomPassword}");
    Console.WriteLine("[SECURITY] IMPORTANT: Change this password immediately after first login!");
    Console.WriteLine(new string('=', 80) + "\n");
}

/// <summary>
/// Authenticates a profile by username and password
/// Returns null if authentication fails
/// </summary>
async Task<Profile?> AuthenticateProfileAsync(string username, string password, ProfileService profileService, HttpContext context)
{
    var profile = await profileService.GetByUsernameAsync(username);
    if (profile == null)
    {
        await profileService.RegisterLoginAttemptAsync(username, false);
        context.Response.Redirect("/login?error=invalid");
        return null;
    }

    // Check if account is locked
    if (profile.LockoutUntil.HasValue && profile.LockoutUntil.Value > DateTimeOffset.UtcNow)
    {
        var remaining = (int)(profile.LockoutUntil.Value - DateTimeOffset.UtcNow).TotalMinutes;
        context.Response.Redirect($"/login?error=locked&minutes={remaining}");
        return null;
    }

    // Verify password
    if (!ProfileService.VerifyPassword(profile, password))
    {
        await profileService.RegisterLoginAttemptAsync(username, false);
        context.Response.Redirect("/login?error=invalid");
        return null;
    }

    // Register successful login
    await profileService.RegisterLoginAttemptAsync(username, true);
    return profile;
}

/// <summary>
/// Signs in a profile by creating authentication claims and session
/// </summary>
async Task SignInProfileAsync(string username, Profile profile, HttpContext context)
{
    var claims = new List<System.Security.Claims.Claim>
    {
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, profile.Role)
    };
    var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new System.Security.Claims.ClaimsPrincipal(identity);

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    context.Response.Redirect("/");
}
