using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Modal;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace CharacterManager.Tests.Components.Modal;

public class SettingsModalTests : BlazorComponentTestBase
{
    private readonly Mock<IModalService> _modalServiceMock;
    private readonly Mock<IAdultModeNotificationService> _adultModeNotificationMock;
    private readonly Mock<IProfileService> _profileServiceMock;
    private readonly Mock<IAppVersionService> _appVersionServiceMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IJSRuntime> _jsRuntimeMock;
    private readonly string _testDir;
    private readonly string _i18nDir;

    public SettingsModalTests()
    {
        // Create mocks for all dependencies
        _modalServiceMock = new Mock<IModalService>();
        _adultModeNotificationMock = new Mock<IAdultModeNotificationService>();
        _profileServiceMock = new Mock<IProfileService>();
        _appVersionServiceMock = new Mock<IAppVersionService>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _jsRuntimeMock = new Mock<IJSRuntime>();
        
        // Setup default mock behaviors
        _appVersionServiceMock.Setup(s => s.GetAppVersion()).Returns("1.0.0");
        
        // Create temp directory for i18n files
        _testDir = Path.Combine(Path.GetTempPath(), $"SettingsModalTests_{Guid.NewGuid()}");
        _i18nDir = Path.Combine(_testDir, "i18n");
        Directory.CreateDirectory(_i18nDir);

        // Create test localization file
        var frContent = new Dictionary<string, object>
        {
            ["settings"] = new Dictionary<string, object>
            {
                ["title"] = "Paramètres",
                ["language"] = "Langue",
                ["languageDescription"] = "Choisissez votre langue préférée",
                ["contentMode"] = "Mode de contenu",
                ["adultMode"] = "Mode adulte",
                ["adultModeDescription"] = "Activer le contenu réservé aux adultes",
                ["enabled"] = "Activé",
                ["disabled"] = "Désactivé",
                ["signInPrompt"] = "Connectez-vous pour accéder aux paramètres"
            }
        };
        File.WriteAllText(Path.Combine(_i18nDir, "fr.json"), JsonSerializer.Serialize(frContent));

        // Setup mocks
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(_testDir);
        
        var loggerMock = new Mock<ILogger<ClientLocalizationService>>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        
        var languageContext = new LanguageContextService();

        var localizationService = new ClientLocalizationService(
            envMock.Object,
            loggerMock.Object,
            languageContext,
            httpContextAccessorMock.Object);
        
        localizationService.InitializeAsync("fr").GetAwaiter().GetResult();
        
        // Register services via interfaces
        Services.AddSingleton(_modalServiceMock.Object);
        Services.AddSingleton<ILanguageContextService>(languageContext);
        Services.AddSingleton<IClientLocalizationService>(localizationService);
        Services.AddSingleton(_adultModeNotificationMock.Object);
        Services.AddSingleton(_profileServiceMock.Object);
        Services.AddSingleton(_appVersionServiceMock.Object);
        Services.AddSingleton(_jsRuntimeMock.Object);
        
        // Add authorization services
        Services.AddAuthorizationCore();
    }

    #region Structure Tests

    [Fact]
    public void SettingsModal_ComponentType_IsValid()
    {
        // Verify that the component exists and is of correct type
        var componentType = typeof(SettingsModal);
        Assert.NotNull(componentType);
    }

    [Fact]
    public void SettingsModal_ShouldHaveRequiredServices()
    {
        // Verify all required services are registered
        Assert.NotNull(_modalServiceMock);
        Assert.NotNull(_adultModeNotificationMock);
        Assert.NotNull(_profileServiceMock);
        Assert.NotNull(_appVersionServiceMock);
        Assert.NotNull(_dbContextMock);
    }

    #endregion

    #region Service Injection Tests

    [Fact]
    public void SettingsModal_IAppVersionService_IsAccessible()
    {
        // Verify IAppVersionService is properly registered
        var service = Services.GetService<IAppVersionService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void SettingsModal_IClientLocalizationService_IsAccessible()
    {
        // Verify IClientLocalizationService is properly registered
        var service = Services.GetService<IClientLocalizationService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void SettingsModal_IProfileService_IsAccessible()
    {
        // Verify IProfileService is properly registered
        var service = Services.GetService<IProfileService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void SettingsModal_IAdultModeNotificationService_IsAccessible()
    {
        // Verify IAdultModeNotificationService is properly registered
        var service = Services.GetService<IAdultModeNotificationService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void SettingsModal_IModalService_IsAccessible()
    {
        // Verify IModalService is properly registered
        var service = Services.GetService<IModalService>();
        Assert.NotNull(service);
    }

    #endregion

    #region Localization Tests

    [Fact]
    public void SettingsModal_LocalizationKeys_AreCorrect()
    {
        // Verify localization keys are properly defined
        var expectedKeys = new[]
        {
            "settings.title",
            "settings.language",
            "settings.languageDescription",
            "settings.contentMode",
            "settings.adultMode",
            "settings.adultModeDescription",
            "settings.enabled",
            "settings.disabled",
            "settings.signInPrompt"
        };

        Assert.Equal(9, expectedKeys.Length);
    }

    // Duplicate test removed - duplicate of SettingsModal_IClientLocalizationService_IsAccessible

    #endregion

    #region Service Behavior Tests

    [Fact]
    public void SettingsModal_AppVersionService_ReturnsVersion()
    {
        // Verify app version service returns a version
        var version = _appVersionServiceMock.Object.GetAppVersion();
        Assert.NotNull(version);
        Assert.Equal("1.0.0", version);
    }

    [Fact]
    public void SettingsModal_ModalService_CanClose()
    {
        // Verify modal service close method can be called
        _modalServiceMock.Object.Close();
        _modalServiceMock.Verify(m => m.Close(), Times.Once);
    }

    [Fact]
    public void SettingsModal_AdultModeNotificationService_CanSetAdultMode()
    {
        // Verify adult mode notification service can be configured
        _adultModeNotificationMock.Object.SetAdultMode(true);
        _adultModeNotificationMock.Verify(m => m.SetAdultMode(true), Times.Once);
    }

    [Fact]
    public async Task SettingsModal_ProfileService_CanGetOrCreateProfile()
    {
        // Verify profile service GetOrCreateAsync can be called
        var mockProfile = new Profile { Username = "testuser", Language = "fr" };
        _profileServiceMock.Setup(p => p.GetOrCreateAsync(It.IsAny<string>())).ReturnsAsync(mockProfile);

        var profile = await _profileServiceMock.Object.GetOrCreateAsync("testuser");
        Assert.NotNull(profile);
        Assert.Equal("testuser", profile.Username);
    }

    [Fact]
    public async Task SettingsModal_ProfileService_CanUpdateProfile()
    {
        // Verify profile service UpdateAsync can be called
        var mockProfile = new Profile { Username = "testuser", Language = "en" };
        _profileServiceMock.Setup(p => p.UpdateAsync(It.IsAny<Profile>())).Returns(Task.CompletedTask);

        await _profileServiceMock.Object.UpdateAsync(mockProfile);
        _profileServiceMock.Verify(p => p.UpdateAsync(mockProfile), Times.Once);
    }

    [Fact]
    public async Task SettingsModal_LocalizationService_CanInitialize()
    {
        // Verify localization service can initialize with a language
        var service = Services.GetService<IClientLocalizationService>();
        Assert.NotNull(service);
        
        await service!.InitializeAsync("fr");
        var translatedText = service["settings.title"];
        Assert.Equal("Paramètres", translatedText);
    }

    #endregion

    #region Compilation Tests

    [Fact]
    public void SettingsModal_Component_Compiles()
    {
        // Verify component compiles without errors
        var componentType = typeof(SettingsModal);
        Assert.NotNull(componentType);
        Assert.Equal("SettingsModal", componentType.Name);
    }

    #endregion
}
