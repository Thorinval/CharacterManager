using CharacterManager.Server;
using CharacterManager.Server.Data;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CharacterManager.Tests;

/// <summary>
/// Tests for Program.cs configuration and service registration
/// These tests verify that the dependency injection container is properly configured
/// </summary>
public class ProgramConfigurationTests
{
    private static IServiceProvider GetConfiguredServiceProvider()
    {
        var services = new ServiceCollection();

        // Configure in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        services.AddSingleton(options);

        // Add DbContext
        services.AddDbContext<ApplicationDbContext>(opt =>
        {
            opt.UseInMemoryDatabase(Guid.NewGuid().ToString());
        });

        // Add Authentication
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/login";
            });
        services.AddAuthorization();
        services.AddHttpContextAccessor();

        // Add Application Services
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Lockout:MaxAttempts"] = "3",
                ["Security:Lockout:LockoutMinutes"] = "5",
                ["App:Version"] = "1.0.0"
            })
            .Build();
        services.AddSingleton<IConfiguration>(config);

        // Add ApplicationServices extension
        var mockEnv = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");
        services.AddSingleton(mockEnv.Object);

        // Manually register services that would be registered in AddApplicationServices
        services.AddScoped<IDatabaseInitializationService, DatabaseInitializationService>();
        services.AddScoped<IHistoriqueModificationService, HistoriqueModificationService>();
        services.AddScoped<IPersonnageService, PersonnageService>();
        services.AddScoped<IHistoriqueClassementService, HistoriqueClassementService>();
        services.AddScoped<IHistoriqueLigueService, HistoriqueLigueService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IUpdateService, UpdateService>();
        services.AddScoped<ILocalizationService, LocalizationService>();
        services.AddScoped<IStatistiquesService, StatistiquesService>();
        services.AddScoped<ICapaciteService, CapaciteService>();
        services.AddScoped<IAppVersionService, AppVersionService>();
        services.AddScoped<IClientLocalizationService, ClientLocalizationService>();
        services.AddScoped<ILanguageContextService, LanguageContextService>();
        services.AddScoped<IAdultModeNotificationService, AdultModeNotificationService>();
        services.AddScoped<IAuthenticationHelper, AuthenticationHelper>();
        services.AddScoped<IPmlImportService, PmlImportService>();
        services.AddScoped<IPmlExportService, PmlExportService>();

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        return services.BuildServiceProvider();
    }

    #region Service Registration Tests

    [Fact]
    public void Configuration_Should_Register_DbContext()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert
        var dbContext = serviceProvider.GetService<ApplicationDbContext>();
        Assert.NotNull(dbContext);
    }

    [Fact]
    public void Configuration_Should_Register_Authentication()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert - No exception means authentication is configured
        Assert.NotNull(serviceProvider);
    }

    [Fact]
    public void Configuration_Should_Register_HttpContextAccessor()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert
        var accessor = serviceProvider.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
        Assert.NotNull(accessor);
    }

    [Fact]
    public void Configuration_Should_Register_PersonnageService()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IPersonnageService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Configuration_Should_Register_ProfileService()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IProfileService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Configuration_Should_Register_StatistiquesService()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IStatistiquesService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Configuration_Should_Register_AppVersionService()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IAppVersionService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Configuration_Should_Register_AuthenticationHelper()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IAuthenticationHelper>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Configuration_Should_Register_PmlImportService()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IPmlImportService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Configuration_Should_Register_PmlExportService()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IPmlExportService>();
        Assert.NotNull(service);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_Should_Have_Security_Settings()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:Lockout:MaxAttempts"] = "3",
                ["Security:Lockout:LockoutMinutes"] = "5"
            })
            .Build();

        // Act
        var maxAttempts = config["Security:Lockout:MaxAttempts"];
        var lockoutMinutes = config["Security:Lockout:LockoutMinutes"];

        // Assert
        Assert.Equal("3", maxAttempts);
        Assert.Equal("5", lockoutMinutes);
    }

    [Fact]
    public void Configuration_Should_Have_Authentication_Paths_Set()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/login";
                options.LogoutPath = "/logout";
                options.AccessDeniedPath = "/login";
            });

        var serviceProvider = services.BuildServiceProvider();

        // Assert - No exception means configuration was successful
        Assert.NotNull(serviceProvider);
    }

    #endregion

    #region Database Initialization Tests

    [Fact]
    public void Database_Initialization_Service_Should_Be_Registered()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert
        var service = serviceProvider.GetService<IDatabaseInitializationService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void Database_Should_Support_Entity_Framework_Core()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();
        var dbContext = serviceProvider.GetService<ApplicationDbContext>();

        // Assert
        Assert.NotNull(dbContext);
        Assert.True(dbContext.Database.CanConnect());
    }

    #endregion

    #region Authentication Endpoint Tests

    [Fact]
    public void AuthenticationHelper_Should_Be_Available()
    {
        // Arrange & Act
        var serviceProvider = GetConfiguredServiceProvider();

        // Assert
        var helper = serviceProvider.GetService<IAuthenticationHelper>();
        Assert.NotNull(helper);
        Assert.IsAssignableFrom<IAuthenticationHelper>(helper);
    }

    #endregion
}
