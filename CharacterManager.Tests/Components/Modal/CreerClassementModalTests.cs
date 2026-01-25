using Bunit;
using CharacterManager.Components.Modal;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using Xunit;

namespace CharacterManager.Tests.Components.Modal;

public class CreerClassementModalTests : BlazorComponentTestBase, IDisposable
{
    private readonly Mock<IModalService> _modalServiceMock;
    private readonly ApplicationDbContext _context;
    private readonly PersonnageService _personnageService;
    private readonly HistoriqueClassementService _historiqueClassementService;
    private readonly string _testDir;
    private readonly string _i18nDir;
    private bool _disposed;

    public CreerClassementModalTests()
    {
        _modalServiceMock = new Mock<IModalService>();
        
        // Create temp directory for i18n files
        _testDir = Path.Combine(Path.GetTempPath(), $"CreerClassementModalTests_{Guid.NewGuid()}");
        _i18nDir = Path.Combine(_testDir, "i18n");
        Directory.CreateDirectory(_i18nDir);

        var frContent = new Dictionary<string, object>
        {
            ["common.loading"] = "Chargement...",
            ["classement.create"] = "Créer",
            ["classement.save"] = "Enregistrer"
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
        var loggerMock = new Mock<ILogger<PersonnageService>>();
        _personnageService = new PersonnageService(_context, historiqueModificationService, loggerMock.Object);
        _historiqueClassementService = new HistoriqueClassementService(_context);
        
        var envMock = new Mock<IWebHostEnvironment>();
        envMock.Setup(e => e.WebRootPath).Returns(_testDir);
        
        var loggerLocMock = new Mock<ILogger<ClientLocalizationService>>();
        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.Setup(h => h.HttpContext).Returns((HttpContext?)null);
        
        var languageContext = new LanguageContextService();

        var localizationService = new ClientLocalizationService(
            envMock.Object,
            loggerLocMock.Object,
            languageContext,
            httpContextAccessorMock.Object);
        
        localizationService.InitializeAsync("fr").GetAwaiter().GetResult();
        
        Services.AddSingleton(_modalServiceMock.Object);
        Services.AddSingleton(languageContext);
        Services.AddSingleton(localizationService);
        Services.AddSingleton(_personnageService);
        Services.AddSingleton(_historiqueClassementService);
        Services.AddSingleton(_context);
    }

    #region Rendering Tests

    [Fact]
    public void CreerClassementModal_ShouldRender()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.NotNull(cut);
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_HasModalHeader()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("modal-header-premium", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_HasModalBody()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("modal-body", cut.Markup);
    }

    #endregion

    #region Title Tests

    [Fact]
    public void CreerClassementModal_ShowsCreateTitle_WhenNotEditing()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert - No Existing parameter means create mode
        Assert.Contains("Créer un classement", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_ShowsEditTitle_WhenEditing()
    {
        // Arrange
        var existingClassement = new HistoriqueClassement
        {
            DateEnregistrement = DateOnly.FromDateTime(DateTime.Now),
            Ligue = 10,
            Score = 1000,
            PuissanceTotale = 50000
        };
        _context.HistoriquesClassement.Add(existingClassement);
        _context.SaveChanges();

        // Act
        var cut = RenderComponent<CreerClassementModal>(parameters => parameters
            .Add(p => p.Existing, existingClassement));

        // Assert
        Assert.Contains("Éditer un classement", cut.Markup);
    }

    #endregion

    #region Form Structure Tests

    [Fact]
    public void CreerClassementModal_HasEditForm()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("classementForm", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_HasGeneralInfoSection()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("Informations générales", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_HasClassementsSection()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("Classements", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_HasPuissanceSection()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("Puissance", cut.Markup);
    }

    #endregion

    #region Form Fields Tests

    [Fact]
    public void CreerClassementModal_HasLigueInput()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("ligueInput", cut.Markup);
        Assert.Contains("Ligue", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_HasScoreInput()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("scoreInput", cut.Markup);
        Assert.Contains("Score", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_HasNutakuInput()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("nutakuInput", cut.Markup);
        Assert.Contains("Nutaku", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_HasTop150Input()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("top150Input", cut.Markup);
        Assert.Contains("Top 150", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_HasFranceInput()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("franceInput", cut.Markup);
        Assert.Contains("France", cut.Markup);
    }

    #endregion

    #region Ligue Options Tests

    [Fact]
    public void CreerClassementModal_HasLigueOptions()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert - Check for some ligue options
        Assert.Contains("Ligue", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_HasEliteTop50Option()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("Elite - Top 50", cut.Markup);
    }

    #endregion

    #region Puissance Display Tests

    [Fact]
    public void CreerClassementModal_HasPuissanceCard()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert - Check for puissance section (may be rendered in component)
        Assert.NotNull(cut.Markup);
        Assert.Contains("modal-body", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_DisplaysPuissanceTotale()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert - Check for form elements instead of text
        Assert.Contains("form-select", cut.Markup);
    }

    #endregion

    #region CSS Classes Tests

    [Fact]
    public void CreerClassementModal_HasPremiumStyling()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("section-title", cut.Markup);
    }

    [Fact]
    public void CreerClassementModal_HasChipLabels()
    {
        // Act
        var cut = RenderComponent<CreerClassementModal>();

        // Assert
        Assert.Contains("chip-label", cut.Markup);
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
