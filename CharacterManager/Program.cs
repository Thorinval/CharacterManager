using CharacterManager.Server.Services;
using CharacterManager.Server;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using CharacterManager.Components;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog from appsettings
builder.Host.UseSerilog((ctx, services, lc) =>
    lc.ReadFrom.Configuration(ctx.Configuration));

// Add all application services with configuration
builder.Services.AddApplicationConfiguration(builder.Environment, builder.Configuration);


var app = builder.Build();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbInitService = scope.ServiceProvider.GetRequiredService<IDatabaseInitializationService>();

    await dbInitService.InitializeDatabaseAsync();
    await dbInitService.InitializeAppSettingsAndCheckStateAsync();
}

// Security pipeline
app.UseAuthentication();
app.UseAuthorization();

// Login endpoint for handling authentication (avoids Blazor Server SignalR conflict)
app.MapPost("/api/login", async (HttpContext context, IAuthenticationHelper authHelper) =>
{
    var form = await context.Request.ReadFormAsync();
    var username = form["username"].ToString();
    var password = form["password"].ToString();

    // Validate input
    var (isValid, errorCode) = authHelper.ValidateLoginInput(username, password);
    if (!isValid)
    {
        context.Response.Redirect($"/login?error={errorCode}");
        return;
    }

    // Bootstrap default admin if no profiles exist
    var generatedPassword = await authHelper.EnsureDefaultAdminExistsAsync();
    if (generatedPassword != null)
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("[SECURITY] No profiles found - created default admin account");
        Console.WriteLine("[SECURITY] Username: admin");
        Console.WriteLine($"[SECURITY] Password: {generatedPassword}");
        Console.WriteLine("[SECURITY] IMPORTANT: Change this password immediately after first login!");
        Console.WriteLine(new string('=', 80) + "\n");
    }

    // Authenticate the user
    var (profile, authErrorCode, lockoutMinutes) = await authHelper.AuthenticateProfileAsync(username!, password!);
    if (profile == null)
    {
        var redirectUrl = authErrorCode == "locked" 
            ? $"/login?error=locked&minutes={lockoutMinutes}" 
            : $"/login?error={authErrorCode}";
        context.Response.Redirect(redirectUrl);
        return;
    }

    // Sign in the user
    await authHelper.SignInProfileAsync(username!, profile, context);
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
