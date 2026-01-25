using Bunit;
using CharacterManager.Components;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CharacterManager.Tests.Components;

public class LocalizedTextComponentTests : TestContext, IDisposable
{
    private readonly string _testDir;
    private readonly string _i18nDir;
    private bool _disposed;

    public LocalizedTextComponentTests()
    {
        // Create temp directory for i18n files
        _testDir = Path.Combine(Path.GetTempPath(), $"LocalizedTextTests_{Guid.NewGuid()}");
        _i18nDir = Path.Combine(_testDir, "i18n");
        Directory.CreateDirectory(_i18nDir);

        // Create test localization file
        var frContent = new Dictionary<string, object>
        {
            ["greeting"] = "Bonjour",
            ["farewell"] = "Au revoir",
            ["nested"] = JsonSerializer.SerializeToElement(new Dictionary<string, string>
            {
                ["key"] = "Valeur imbriquée"
            })
        };
        File.WriteAllText(Path.Combine(_i18nDir, "fr.json"), JsonSerializer.Serialize(frContent));

        // Setup mocks
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(_testDir);
        
        var loggerMock = new Mock<ILogger<ClientLocalizationService>>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        
        var languageContext = new LanguageContextService();

        // Create and initialize the localization service
        var localizationService = new ClientLocalizationService(
            envMock.Object,
            loggerMock.Object,
            languageContext,
            httpContextAccessorMock.Object);
        
        localizationService.InitializeAsync("fr").GetAwaiter().GetResult();
        
        // Register all services BEFORE any component is rendered
        Services.AddSingleton(languageContext);
        Services.AddSingleton(localizationService);
        
        // Setup JSInterop
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    #region Rendering Tests

    [Fact]
    public void LocalizedText_ShouldRenderTranslatedValue()
    {
        // Act
        var cut = RenderComponent<LocalizedText>(parameters => parameters
            .Add(p => p.Key, "greeting"));

        // Assert
        Assert.Equal("Bonjour", cut.Markup.Trim());
    }

    [Fact]
    public void LocalizedText_ShouldRenderKey_WhenKeyNotFound()
    {
        // Act
        var cut = RenderComponent<LocalizedText>(parameters => parameters
            .Add(p => p.Key, "missing.key"));

        // Assert
        Assert.Equal("missing.key", cut.Markup.Trim());
    }

    [Fact]
    public void LocalizedText_ShouldRenderNestedKey()
    {
        // Act
        var cut = RenderComponent<LocalizedText>(parameters => parameters
            .Add(p => p.Key, "nested.key"));

        // Assert
        Assert.Equal("Valeur imbriquée", cut.Markup.Trim());
    }

    #endregion

    #region Parameter Tests

    [Fact]
    public void LocalizedText_ShouldAcceptKeyParameter()
    {
        // Act
        var cut = RenderComponent<LocalizedText>(parameters => parameters
            .Add(p => p.Key, "farewell"));

        // Assert
        Assert.Equal("Au revoir", cut.Markup.Trim());
    }

    [Fact]
    public void LocalizedText_ShouldHandleEmptyKey()
    {
        // Act
        var cut = RenderComponent<LocalizedText>(parameters => parameters
            .Add(p => p.Key, ""));

        // Assert - empty key returns empty string
        Assert.Equal("", cut.Markup.Trim());
    }

    #endregion

    public new void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected new virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                base.Dispose();
                
                if (Directory.Exists(_testDir))
                {
                    try
                    {
                        Directory.Delete(_testDir, recursive: true);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            _disposed = true;
        }
    }
}
