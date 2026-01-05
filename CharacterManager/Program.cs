using CharacterManager.Server.Data;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using CharacterManager.Components;

var builder = WebApplication.CreateBuilder(args);

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
    options.UseSqlite("Data Source=charactermanager.db"));

// Register ProfileService BEFORE PersonnageService (dependency order)
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<PersonnageService>();
builder.Services.AddScoped<PmlImportService>();
builder.Services.AddScoped<HistoriqueClassementService>();
builder.Services.AddScoped<HistoriqueLigueService>();
builder.Services.AddScoped<CapaciteService>();
builder.Services.AddScoped<ClientLocalizationService>();

// AppImageService no longer used for categorization; DI registration removed
builder.Services.AddSingleton<PersonnageImageConfigService>();
builder.Services.AddSingleton<AppVersionService>();
builder.Services.AddSingleton<LocalizationService>();
builder.Services.AddSingleton<LanguageContextService>();  // Service de contexte de langue
builder.Services.AddSingleton<AdultModeNotificationService>();  // Service singleton pour notification mode adulte
builder.Services.AddSingleton<IModalService, ModalService>();
builder.Services.AddScoped<DatabaseInitializationService>();


builder.Services.AddHttpClient<UpdateService>();
builder.Services.AddHttpClient();  // Pour les appels HTTP du ClientLocalizationService

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
app.MapPost("/api/login", async (HttpContext context, ProfileService profileService) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
    {
        context.Response.Redirect("/login?error=required");
        return;
    }

    // Bootstrap default admin if no profiles exist
    var allProfiles = await profileService.GetAllAsync();
    if (allProfiles == null || !allProfiles.Any())
    {
        await profileService.CreateUserAsync("admin", "admin", "admin");
        Console.WriteLine("[Login] No profiles found - created default admin account (admin/admin)");
    }

    var profile = await profileService.GetByUsernameAsync(username);
    if (profile == null)
    {
        await profileService.RegisterLoginAttemptAsync(username, false);
        context.Response.Redirect("/login?error=invalid");
        return;
    }

    if (profile.LockoutUntil.HasValue && profile.LockoutUntil.Value > DateTimeOffset.UtcNow)
    {
        var remaining = (int)(profile.LockoutUntil.Value - DateTimeOffset.UtcNow).TotalMinutes;
        context.Response.Redirect($"/login?error=locked&minutes={remaining}");
        return;
    }

    if (!ProfileService.VerifyPassword(profile, password))
    {
        await profileService.RegisterLoginAttemptAsync(username, false);
        context.Response.Redirect("/login?error=invalid");
        return;
    }

    await profileService.RegisterLoginAttemptAsync(username, true);

    var claims = new List<System.Security.Claims.Claim>
    {
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, profile.Role)
    };
    var identity = new System.Security.Claims.ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new System.Security.Claims.ClaimsPrincipal(identity);

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    context.Response.Redirect("/");
});

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
