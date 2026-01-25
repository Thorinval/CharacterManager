using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Layout;
using CharacterManager.Components.Modal;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CharacterManager.Tests.Components.Layout;

public class MainLayoutTests : TestContext
{
    private readonly Mock<IAppVersionService> _versionServiceMock = new();
    private readonly Mock<IModalService> _modalServiceMock = new();
    private readonly Mock<IUpdateService> _updateServiceMock = new();
    private readonly Mock<IClientLocalizationService> _localizationServiceMock = new();

    public MainLayoutTests()
    {
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("tester");
        authContext.SetRoles("admin");

        _versionServiceMock.Setup(v => v.GetAppVersion()).Returns("9.9.9");
        _localizationServiceMock.Setup(l => l.GetKeyValue(It.IsAny<string>())).Returns<string>(k => k);
        _localizationServiceMock.SetupGet(l => l.CurrentLanguage).Returns("fr");
        Services.AddSingleton(_versionServiceMock.Object);
        Services.AddSingleton(_modalServiceMock.Object);
        Services.AddSingleton(_updateServiceMock.Object);
        Services.AddSingleton(_localizationServiceMock.Object);
        Services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor());
        Services.AddSingleton<Microsoft.AspNetCore.Authentication.IAuthenticationService>(Mock.Of<Microsoft.AspNetCore.Authentication.IAuthenticationService>());

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new ApplicationDbContext(options);
        Services.AddSingleton(db);
        Services.AddSingleton<IConfiguration>(config);

        var profile = new Profile { Username = "tester", Language = "en", Role = "admin" };
        db.Profiles.Add(profile);
        db.SaveChanges();

        var profileService = new ProfileService(db, config, NullLogger<ProfileService>.Instance);
        Services.AddSingleton<IProfileService>(profileService);

        _updateServiceMock.Setup(s => s.CheckForUpdatesAsync()).ReturnsAsync(new UpdateInfo { IsUpdateAvailable = false });

        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Displays_user_name_role_and_version()
    {
        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<MainLayout>());

        cut.WaitForAssertion(() => Assert.Contains("tester", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("ADMIN", cut.Markup));
        cut.WaitForAssertion(() => Assert.Contains("9.9.9", cut.Markup));
    }

    [Fact]
    public void Logout_navigates_to_api_logout()
    {
        var nav = Services.GetRequiredService<NavigationManager>();

        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<MainLayout>());

        var logoutButton = cut.Find("button.logout-btn");
        logoutButton.Click();

        cut.WaitForAssertion(() => Assert.EndsWith("/api/logout", nav.Uri));
    }

    [Fact]
    public void Clicking_modals_calls_modal_service()
    {
        var cut = RenderComponent<CascadingAuthenticationState>(parameters =>
            parameters.AddChildContent<MainLayout>());

        cut.Find("span[title='À propos']").Click();
        cut.Find("span[title='Feuille de route']").Click();
        cut.Find("span[title='Notes de version']").Click();
        cut.Find("span[title='Paramètres']").Click();

        _modalServiceMock.Verify(m => m.Open<AboutModal>(It.IsAny<Dictionary<string, object>?>(), ModalSize.Medium), Times.Once);
        _modalServiceMock.Verify(m => m.Open<RoadmapModal>(It.IsAny<Dictionary<string, object>?>(), ModalSize.XL), Times.Once);
        _modalServiceMock.Verify(m => m.Open<ChangelogModal>(It.IsAny<Dictionary<string, object>?>(), ModalSize.XL), Times.Once);
        _modalServiceMock.Verify(m => m.Open<SettingsModal>(It.IsAny<Dictionary<string, object>?>(), ModalSize.Large), Times.Once);
    }
}
