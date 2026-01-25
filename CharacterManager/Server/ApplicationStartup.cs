using CharacterManager.Server.Data;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace CharacterManager.Server;

/// <summary>
/// Handles application service configuration and setup
/// </summary>
public static class ApplicationStartup
{
    /// <summary>
    /// Configures all application services with dependency injection
    /// </summary>
    public static IServiceCollection AddApplicationConfiguration(
        this IServiceCollection services,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        // Configure Serilog (already configured in Program.cs but available here for testing)
        // Note: Serilog is configured in Program.cs via builder.Host.UseSerilog()

        // Add Razor components and interactive server components
        services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Authentication & Authorization
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/login";
            });

        services.AddAuthorization();
        services.AddHttpContextAccessor();

        // Configure SQLite database
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseSqlite("Data Source=charactermanager.db");

            // Enable detailed logging in development
            if (environment.IsDevelopment())
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

        // Register IApplicationDbContext for dependency injection
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // Register application services (already handles ProfileService BEFORE PersonnageService ordering)
        services.AddApplicationServices(environment);

        // API controllers
        services.AddControllers();

        return services;
    }

    /// <summary>
    /// Verifies that all required services are registered
    /// </summary>
    public static bool VerifyServicesRegistered(IServiceProvider serviceProvider)
    {
        try
        {
            // Verify core services
            var dbContext = serviceProvider.GetRequiredService<IApplicationDbContext>();
            var profileService = serviceProvider.GetRequiredService<IProfileService>();
            var authHelper = serviceProvider.GetRequiredService<IAuthenticationHelper>();
            var databaseInitService = serviceProvider.GetRequiredService<IDatabaseInitializationService>();

            // Verify interface services exist
            var appVersionService = serviceProvider.GetRequiredService<IAppVersionService>();
            var localizationService = serviceProvider.GetRequiredService<IClientLocalizationService>();
            var modalService = serviceProvider.GetRequiredService<IModalService>();

            return dbContext != null &&
                   profileService != null &&
                   authHelper != null &&
                   databaseInitService != null &&
                   appVersionService != null &&
                   localizationService != null &&
                   modalService != null;
        }
        catch
        {
            return false;
        }
    }
}
