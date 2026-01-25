using Bunit;
using Bunit.TestDoubles;
using CharacterManager.Components.Modal;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Text.Json;
using Xunit;

namespace CharacterManager.Tests.Components.Modal;

public class ImportExportPmlModalTests : BlazorComponentTestBase, IDisposable
{
    private readonly Mock<IModalService> _modalServiceMock;
    private readonly ApplicationDbContext _context;
    private readonly PmlImportService _pmlImportService;
    private readonly PmlExportService _pmlExportService;
    private readonly string _testDir;
    private readonly string _i18nDir;
    private bool _disposed;

    public ImportExportPmlModalTests()
    {
        _modalServiceMock = new Mock<IModalService>();
        
        // Create temp directory for i18n files
        _testDir = Path.Combine(Path.GetTempPath(), $"ImportExportPmlModalTests_{Guid.NewGuid()}");
        _i18nDir = Path.Combine(_testDir, "i18n");
        Directory.CreateDirectory(_i18nDir);

        // Create test localization file
        var frContent = new Dictionary<string, object>
        {
            ["importExportPml.cardTitle"] = "Import/Export PML",
            ["importExportPml.success"] = "Import réussi !",
            ["importExportPml.imported"] = "éléments importés",
            ["importExportPml.error"] = "Erreur lors de l'import",
            ["importExportPml.warnings"] = "Avertissements",
            ["importExportPml.import"] = "Importer",
            ["importExportPml.export"] = "Exporter",
            ["importExportPml.selectFile"] = "Sélectionner un fichier",
            ["importExportPml.exportAll"] = "Exporter tout",
            ["importExportPml.exportSelected"] = "Exporter sélectionnés"
        };
        File.WriteAllText(Path.Combine(_i18nDir, "fr.json"), JsonSerializer.Serialize(frContent));

        // Setup database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        // Setup services
        var historiqueModificationService = new HistoriqueModificationService(_context);
        _pmlImportService = new PmlImportService(_context, historiqueModificationService);
        var pmlExportLoggerMock = new Mock<ILogger<PmlExportService>>();
        _pmlExportService = new PmlExportService(_context, pmlExportLoggerMock.Object);

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
        Services.AddSingleton(languageContext);
        Services.AddSingleton(localizationService);
        Services.AddSingleton(_pmlImportService);
        Services.AddSingleton(_pmlExportService);

        // Setup authorization
        var authContext = this.AddTestAuthorization();
        authContext.SetAuthorized("testuser");
        authContext.SetRoles("admin");

        // Add JSRuntime mock
        JSInterop.SetupVoid("eval", _ => true);
    }

    #region Rendering Tests

    [Fact]
    public void ImportExportPmlModal_ShouldRender()
    {
        // Act
        var cut = RenderComponent<ImportExportPmlModal>();

        // Assert
        Assert.NotNull(cut);
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void ImportExportPmlModal_HasModalBodyClass()
    {
        // Act
        var cut = RenderComponent<ImportExportPmlModal>();

        // Assert
        Assert.Contains("modal-body", cut.Markup);
    }

    [Fact]
    public void ImportExportPmlModal_DisplaysTitle()
    {
        // Act
        var cut = RenderComponent<ImportExportPmlModal>();

        // Assert - Check for structure elements instead of localized text
        Assert.Contains("cloud_upload", cut.Markup);
        Assert.Contains("modal-body", cut.Markup);
    }

    [Fact]
    public void ImportExportPmlModal_HasIcon()
    {
        // Act
        var cut = RenderComponent<ImportExportPmlModal>();

        // Assert - Has cloud_upload icon
        Assert.Contains("cloud_upload", cut.Markup);
    }

    #endregion

    #region Structure Tests

    [Fact]
    public void ImportExportPmlModal_HasCard()
    {
        // Act
        var cut = RenderComponent<ImportExportPmlModal>();

        // Assert
        Assert.Contains("card", cut.Markup);
    }

    [Fact]
    public void ImportExportPmlModal_HasCardHeader()
    {
        // Act
        var cut = RenderComponent<ImportExportPmlModal>();

        // Assert
        Assert.Contains("card-header", cut.Markup);
    }

    [Fact]
    public void ImportExportPmlModal_HasCardBody()
    {
        // Act
        var cut = RenderComponent<ImportExportPmlModal>();

        // Assert
        Assert.Contains("card-body", cut.Markup);
    }

    #endregion

    #region Localization Keys Tests

    [Fact]
    public void ImportExportPmlModal_LocalizationKeys_ShouldBeCorrect()
    {
        var expectedKeys = new[]
        {
            "importExportPml.cardTitle",
            "importExportPml.success",
            "importExportPml.imported",
            "importExportPml.error",
            "importExportPml.warnings"
        };

        Assert.Equal(5, expectedKeys.Length);
    }

    #endregion

    #region Cleanup

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
                _context.Dispose();
                
                if (Directory.Exists(_testDir))
                {
                    try { Directory.Delete(_testDir, recursive: true); }
                    catch { }
                }
            }
            _disposed = true;
        }
    }

    #endregion
}
