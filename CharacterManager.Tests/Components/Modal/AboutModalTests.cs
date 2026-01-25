using Bunit;
using CharacterManager.Components.Modal;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CharacterManager.Tests.Components.Modal;

public class AboutModalTests : BlazorComponentTestBase
{
    private readonly Mock<IModalService> _modalServiceMock;
    private readonly AppVersionService _versionService;
    private readonly string _testDir;
    private readonly string _i18nDir;

    public AboutModalTests()
    {
        _modalServiceMock = new Mock<IModalService>();
        
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppInfo:Name"] = "TestApp",
                ["AppInfo:Version"] = "1.2.3",
                ["AppInfo:Author"] = "Test Author",
                ["AppInfo:Description"] = "Test Description"
            })
            .Build();
        
        _versionService = new AppVersionService(configuration);
        
        // Create temp directory for i18n files
        _testDir = Path.Combine(Path.GetTempPath(), $"AboutModalTests_{Guid.NewGuid()}");
        _i18nDir = Path.Combine(_testDir, "i18n");
        Directory.CreateDirectory(_i18nDir);

        // Create test localization file
        var frContent = new Dictionary<string, object>
        {
            ["about.title"] = "À propos"
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
        
        Services.AddSingleton(_modalServiceMock.Object);
        Services.AddSingleton<IAppVersionService>(_versionService);
        Services.AddSingleton<ILanguageContextService>(languageContext);
        Services.AddSingleton<IClientLocalizationService>(localizationService);
    }

    #region Rendering Tests

    [Fact]
    public void AboutModal_ShouldRenderModalBody()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        var modalBody = cut.Find(".modal-body");
        Assert.NotNull(modalBody);
    }

    [Fact]
    public void AboutModal_ShouldDisplayAppName()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        Assert.Contains("TestApp", cut.Markup);
    }

    [Fact]
    public void AboutModal_ShouldDisplayAppVersion()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        Assert.Contains("1.2.3", cut.Markup);
    }

    [Fact]
    public void AboutModal_ShouldDisplayAuthor()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        Assert.Contains("Test Author", cut.Markup);
    }

    [Fact]
    public void AboutModal_ShouldDisplayDescription()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        Assert.Contains("Test Description", cut.Markup);
    }

    [Fact]
    public void AboutModal_ShouldDisplayInfoIcon()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        var icon = cut.Find("i.bi-info-circle");
        Assert.NotNull(icon);
    }

    [Fact]
    public void AboutModal_ShouldDisplayCopyrightYear()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        Assert.Contains(DateTime.Now.Year.ToString(), cut.Markup);
    }

    [Fact]
    public void AboutModal_ShouldHaveVersionInfo()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        var versionInfo = cut.Find(".version-info");
        Assert.NotNull(versionInfo);
    }

    [Fact]
    public void AboutModal_ShouldHaveAuthorInfo()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        var authorInfo = cut.Find(".author-info");
        Assert.NotNull(authorInfo);
    }

    #endregion

    #region Content Tests

    [Fact]
    public void AboutModal_ShouldDisplayVersionLabel()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        Assert.Contains("Version:", cut.Markup);
    }

    [Fact]
    public void AboutModal_ShouldDisplayBuildLabel()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        Assert.Contains("Build:", cut.Markup);
    }

    [Fact]
    public void AboutModal_ShouldDisplayCommitLabel()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        Assert.Contains("Commit:", cut.Markup);
    }

    [Fact]
    public void AboutModal_ShouldDisplayDeveloppePar()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        Assert.Contains("Développé par:", cut.Markup);
    }

    [Fact]
    public void AboutModal_ShouldDisplayCopyrightNotice()
    {
        // Act
        var cut = RenderComponent<AboutModal>();

        // Assert
        Assert.Contains("Tous droits réservés", cut.Markup);
    }

    #endregion
}
