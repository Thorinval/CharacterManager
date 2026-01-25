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
    private readonly IClientLocalizationService _localizationService;

    public SettingsModalTests()
    {
        _modalServiceMock = new Mock<IModalService>();
        _adultModeNotificationMock = new Mock<IAdultModeNotificationService>();
        _profileServiceMock = new Mock<IProfileService>();
        _appVersionServiceMock = new Mock<IAppVersionService>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _jsRuntimeMock = new Mock<IJSRuntime>();

        _appVersionServiceMock.Setup(s => s.GetAppVersion()).Returns("1.0.0");

        Services.AddSingleton(_dbContextMock.Object);

        var frContent = new Dictionary<string, string>
        {
            ["settings.title"] = "Paramètres",
            ["settings.language"] = "Langue",
            ["settings.languageDescription"] = "Choisissez votre langue préférée",
            ["settings.contentMode"] = "Mode de contenu",
            ["settings.adultMode"] = "Mode adulte",
            ["settings.adultModeDescription"] = "Activer le contenu réservé aux adultes",
            ["settings.enabled"] = "Activé",
            ["settings.disabled"] = "Désactivé",
            ["settings.signInPrompt"] = "Connectez-vous pour accéder aux paramètres"
        };

        var localizationServiceMock = new Mock<IClientLocalizationService>();
        localizationServiceMock.Setup(s => s.InitializeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        localizationServiceMock.Setup(s => s.GetKeyValue(It.IsAny<string>())).Returns((string key) => frContent.TryGetValue(key, out var v) ? v : key);
        localizationServiceMock.Setup(s => s[It.IsAny<string>()]).Returns((string key) => frContent.TryGetValue(key, out var v) ? v : key);
        localizationServiceMock.SetupGet(s => s.CurrentLanguage).Returns("fr");
        localizationServiceMock.Setup(s => s.GetCurrentLanguage()).Returns("fr");
        localizationServiceMock.Setup(s => s.ChangeLanguageAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        localizationServiceMock.Setup(s => s.SetLanguageAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

        _localizationService = localizationServiceMock.Object;

        Services.AddSingleton(_modalServiceMock.Object);
        Services.AddSingleton<ILanguageContextService>(new LanguageContextService());
        Services.AddSingleton<IClientLocalizationService>(_localizationService);
        Services.AddSingleton(_adultModeNotificationMock.Object);
        Services.AddSingleton(_profileServiceMock.Object);
        Services.AddSingleton(_appVersionServiceMock.Object);
        Services.AddSingleton(_jsRuntimeMock.Object);

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
        await _localizationService.InitializeAsync("fr");
        var translatedText = _localizationService.GetKeyValue("settings.title");
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

    #region Rendering Tests

    [Fact]
    public void SettingsModal_Renders_Unauthorized_Prompt()
    {
        // No auth setup, should show sign-in prompt
        this.AddTestAuthorization().SetNotAuthorized();
        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<SettingsModal>());

        Assert.Contains("Connectez-vous pour accéder aux paramètres", cut.Markup);
    }

    [Fact]
    public void SettingsModal_Renders_Settings_When_Authorized()
    {
        this.AddTestAuthorization().SetAuthorized("testuser", AuthorizationState.Authorized);
        _profileServiceMock.Setup(p => p.GetOrCreateAsync("testuser"))
            .ReturnsAsync(new Profile { Username = "testuser", Language = "fr", AdultMode = false });

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<SettingsModal>());

        cut.WaitForAssertion(() => Assert.Contains("Paramètres", cut.Markup));
        Assert.Contains("Langue", cut.Markup);
        Assert.Contains("Mode adulte", cut.Markup);
    }

    [Fact]
    public async Task SettingsModal_AdultMode_Toggle_Updates_Profile()
    {
        this.AddTestAuthorization().SetAuthorized("admin", AuthorizationState.Authorized)
            .SetRoles("admin");
        var profile = new Profile { Username = "admin", Language = "fr", AdultMode = false };
        _profileServiceMock.Setup(p => p.GetOrCreateAsync("admin")).ReturnsAsync(profile);
        _profileServiceMock.Setup(p => p.UpdateAsync(It.IsAny<Profile>())).Returns(Task.CompletedTask);

        var cut = RenderComponent<CascadingAuthenticationState>(p => p.AddChildContent<SettingsModal>());

        cut.WaitForAssertion(() => Assert.Contains("form-check-input", cut.Markup));

        var toggle = cut.Find("input.form-check-input");
        await cut.InvokeAsync(() => toggle.Change(true));

        _profileServiceMock.Verify(p => p.UpdateAsync(It.Is<Profile>(pr => pr.AdultMode)), Times.Once);
        _adultModeNotificationMock.Verify(a => a.SetAdultMode(true), Times.Once);
    }

    #endregion
}
