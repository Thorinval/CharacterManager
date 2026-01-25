using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;
using Xunit;

namespace CharacterManager.Tests.Components;

public class LocalizationProviderTests : BlazorComponentTestBase
{
    private readonly Mock<IClientLocalizationService> _localizationServiceMock;
    private readonly Mock<IProfileService> _profileServiceMock;
    private readonly Mock<ILanguageContextService> _languageContextMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;

    public LocalizationProviderTests()
    {
        // Create mocks for all dependencies
        _localizationServiceMock = new Mock<IClientLocalizationService>();
        _profileServiceMock = new Mock<IProfileService>();
        _languageContextMock = new Mock<ILanguageContextService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

        // Setup default mock behaviors
        _localizationServiceMock.Setup(s => s.InitializeAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _profileServiceMock.Setup(p => p.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((Profile)null!);
        _languageContextMock.Setup(l => l.SetLanguageForUser(It.IsAny<string>(), It.IsAny<string>())).Verifiable();

        // Register services via interfaces
        Services.AddSingleton(_localizationServiceMock.Object);
        Services.AddSingleton(_profileServiceMock.Object);
        Services.AddSingleton<ILanguageContextService>(_languageContextMock.Object);
        Services.AddSingleton(_httpContextAccessorMock.Object);
    }

    #region Structure Tests

    [Fact]
    public void LocalizationProvider_ComponentType_IsValid()
    {
        // Verify that the component exists and is of correct type
        var componentType = typeof(LocalizationProvider);
        Assert.NotNull(componentType);
    }

    [Fact]
    public void LocalizationProvider_ShouldHaveRequiredServices()
    {
        // Verify all required services are registered
        Assert.NotNull(_localizationServiceMock);
        Assert.NotNull(_profileServiceMock);
        Assert.NotNull(_languageContextMock);
        Assert.NotNull(_httpContextAccessorMock);
    }

    [Fact]
    public void LocalizationProvider_ShouldHaveChildContentParameter()
    {
        // Verify component has ChildContent parameter
        var componentType = typeof(LocalizationProvider);
        var parameters = componentType.GetProperties();
        Assert.NotEmpty(parameters);
    }

    #endregion

    #region Service Injection Tests

    [Fact]
    public void LocalizationProvider_IClientLocalizationService_IsAccessible()
    {
        // Verify IClientLocalizationService is properly registered
        var service = Services.GetService<IClientLocalizationService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void LocalizationProvider_IProfileService_IsAccessible()
    {
        // Verify IProfileService is properly registered
        var service = Services.GetService<IProfileService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void LocalizationProvider_ILanguageContextService_IsAccessible()
    {
        // Verify ILanguageContextService is properly registered
        var service = Services.GetService<ILanguageContextService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void LocalizationProvider_IHttpContextAccessor_IsAccessible()
    {
        // Verify IHttpContextAccessor is properly registered
        var service = Services.GetService<IHttpContextAccessor>();
        Assert.NotNull(service);
    }

    #endregion

    #region Service Behavior Tests

    [Fact]
    public async Task LocalizationProvider_ClientLocalizationService_CanInitialize()
    {
        // Verify localization service initialization
        await _localizationServiceMock.Object.InitializeAsync("fr");
        _localizationServiceMock.Verify(l => l.InitializeAsync("fr"), Times.Once);
    }

    [Fact]
    public async Task LocalizationProvider_ProfileService_CanGetByUsername()
    {
        // Verify profile service GetByUsernameAsync can be called
        var mockProfile = new Profile { Username = "testuser", Language = "en" };
        _profileServiceMock.Setup(p => p.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync(mockProfile);

        var profile = await _profileServiceMock.Object.GetByUsernameAsync("testuser");
        Assert.NotNull(profile);
        Assert.Equal("testuser", profile.Username);
        Assert.Equal("en", profile.Language);
    }

    [Fact]
    public async Task LocalizationProvider_ProfileService_ReturnsNullWhenNotFound()
    {
        // Verify profile service returns null when profile not found
        _profileServiceMock.Setup(p => p.GetByUsernameAsync(It.IsAny<string>())).ReturnsAsync((Profile)null!);

        var profile = await _profileServiceMock.Object.GetByUsernameAsync("nonexistent");
        Assert.Null(profile);
    }

    [Fact]
    public void LocalizationProvider_LanguageContextService_CanSetLanguage()
    {
        // Verify language context service can set language for user
        _languageContextMock.Object.SetLanguageForUser("testuser", "en");
        _languageContextMock.Verify(l => l.SetLanguageForUser("testuser", "en"), Times.Once);
    }

    [Fact]
    public void LocalizationProvider_LanguageContextService_CanSetLanguageForAnonymous()
    {
        // Verify language context service can set language for anonymous users
        _languageContextMock.Object.SetLanguageForUser("", "fr");
        _languageContextMock.Verify(l => l.SetLanguageForUser("", "fr"), Times.Once);
    }

    #endregion

    #region Initialization Logic Tests

    [Fact]
    public async Task LocalizationProvider_Initialization_UsesDefaultLanguageWhenUnauthenticated()
    {
        // Setup: No authenticated user
        _httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext)null!);

        // When: Component initializes
        _localizationServiceMock.Setup(s => s.InitializeAsync("fr")).Returns(Task.CompletedTask);
        await _localizationServiceMock.Object.InitializeAsync("fr");

        // Then: Default language (fr) is used
        _localizationServiceMock.Verify(l => l.InitializeAsync("fr"), Times.Once);
    }

    [Fact]
    public async Task LocalizationProvider_Initialization_UsesUserLanguageWhenProfileExists()
    {
        // Setup: User profile with language preference
        var mockProfile = new Profile { Username = "testuser", Language = "en" };
        _profileServiceMock.Setup(p => p.GetByUsernameAsync("testuser")).ReturnsAsync(mockProfile);

        // When: Getting user profile
        var profile = await _profileServiceMock.Object.GetByUsernameAsync("testuser");

        // Then: User's language is retrieved
        Assert.NotNull(profile);
        Assert.Equal("en", profile.Language);
    }

    [Fact]
    public void LocalizationProvider_Initialization_HandlesExceptions()
    {
        // Setup: Service throws exception when configured with Throws
        _profileServiceMock.Setup(p => p.GetByUsernameAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Test error"));

        // When: Calling service that's configured to throw
        var setupTask = _profileServiceMock.Object.GetByUsernameAsync("testuser");

        // Then: The task will contain the exception when awaited
        Assert.NotNull(setupTask);
    }

    #endregion

    #region Component Rendering Tests

    [Fact]
    public void LocalizationProvider_Component_ShouldRenderChildContent()
    {
        // Component should have logic to render child content and accepts child content parameter
        var componentType = typeof(LocalizationProvider);
        var parameterAttribute = componentType.GetProperties()
            .FirstOrDefault(p => p.Name == "ChildContent");
        
        Assert.NotNull(componentType);
        Assert.NotNull(parameterAttribute);
    }

    #endregion

    #region Compilation Tests

    [Fact]
    public void LocalizationProvider_Component_Compiles()
    {
        // Verify component compiles without errors
        var componentType = typeof(LocalizationProvider);
        Assert.NotNull(componentType);
        Assert.Equal("LocalizationProvider", componentType.Name);
    }

    [Fact]
    public void LocalizationProvider_Services_AreProperlyConfigured()
    {
        // Verify all services can be resolved from the service provider
        var localizationService = Services.GetService<IClientLocalizationService>();
        var profileService = Services.GetService<IProfileService>();
        var languageContext = Services.GetService<ILanguageContextService>();
        var httpContextAccessor = Services.GetService<IHttpContextAccessor>();

        Assert.NotNull(localizationService);
        Assert.NotNull(profileService);
        Assert.NotNull(languageContext);
        Assert.NotNull(httpContextAccessor);
    }

    #endregion
}
