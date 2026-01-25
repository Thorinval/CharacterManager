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

public class DetailPersonnageModalTests : BlazorComponentTestBase
{
    private readonly Mock<IModalService> _modalServiceMock;
    private readonly ApplicationDbContext _context;
    private readonly PersonnageService _personnageService;
    private readonly string _testDir;
    private readonly string _i18nDir;

    public DetailPersonnageModalTests()
    {
        _modalServiceMock = new Mock<IModalService>();
        
        // Create temp directory for i18n files
        _testDir = Path.Combine(Path.GetTempPath(), $"DetailPersonnageModalTests_{Guid.NewGuid()}");
        _i18nDir = Path.Combine(_testDir, "i18n");
        Directory.CreateDirectory(_i18nDir);

        // Create test localization file
        var frContent = new Dictionary<string, object>
        {
            ["common.loading"] = "Chargement...",
            ["personnage.nom"] = "Nom",
            ["personnage.rarete"] = "Rareté"
        };
        File.WriteAllText(Path.Combine(_i18nDir, "fr.json"), JsonSerializer.Serialize(frContent));

        // Setup database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        // Setup services
        var historiqueService = new HistoriqueModificationService(_context);
        var loggerMock = new Mock<ILogger<PersonnageService>>();
        _personnageService = new PersonnageService(_context, historiqueService, loggerMock.Object);
        
        // Setup environment and localization
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
        Services.AddSingleton<ILanguageContextService>(languageContext);
        Services.AddSingleton<IClientLocalizationService>(localizationService);
        Services.AddSingleton<IPersonnageService>(_personnageService);
        Services.AddSingleton<IHistoriqueModificationService, HistoriqueModificationService>(_ => historiqueService);
        
        // Add JSRuntime mock
        JSInterop.SetupVoid("eval", _ => true);
        JSInterop.SetupVoid("alert", _ => true);
    }

    private Personnage CreateTestPersonnage()
    {
        var personnage = new Personnage
        {
            Nom = "Test Personnage",
            Rarete = Rarete.SSR,
            Type = TypePersonnage.Commandant,
            Role = Role.Combattante,
            Faction = Faction.Syndicat,
            TypeAttaque = TypeAttaque.Melee,
            Rang = 5,
            Niveau = 100,
            Puissance = 50000,
            PA = 1000,
            PV = 10000
        };
        _context.Personnages.Add(personnage);
        _context.SaveChanges();
        return personnage;
    }

    #region Parameter Tests

    [Fact]
    public void DetailPersonnageModal_ShouldRender_WithPersonnageId()
    {
        // Arrange
        var personnage = CreateTestPersonnage();

        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert
        Assert.NotNull(cut);
        Assert.NotNull(cut.Markup);
    }

    [Fact]
    public void DetailPersonnageModal_ShouldDisplayPersonnageName()
    {
        // Arrange
        var personnage = CreateTestPersonnage();

        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert
        Assert.Contains("Test Personnage", cut.Markup);
    }

    #endregion

    #region Structure Tests

    [Fact]
    public void DetailPersonnageModal_HasModalBodyClass()
    {
        // Arrange
        var personnage = CreateTestPersonnage();

        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert
        Assert.Contains("modal-body", cut.Markup);
    }

    [Fact]
    public void DetailPersonnageModal_HasPageHeaderBanner()
    {
        // Arrange
        var personnage = CreateTestPersonnage();

        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert
        Assert.Contains("page-header-banner", cut.Markup);
    }

    [Fact]
    public void DetailPersonnageModal_HasCapacitesButton()
    {
        // Arrange
        var personnage = CreateTestPersonnage();

        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert - Button with capacités text
        Assert.Contains("Capacités", cut.Markup);
    }

    [Fact]
    public void DetailPersonnageModal_HasEditButton()
    {
        // Arrange
        var personnage = CreateTestPersonnage();

        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert - Has edit button
        Assert.Contains("Éditer", cut.Markup);
    }

    #endregion

    #region Display Tests

    [Fact]
    public void DetailPersonnageModal_DisplaysRarete()
    {
        // Arrange
        var personnage = CreateTestPersonnage();

        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert
        Assert.Contains("SSR", cut.Markup);
    }

    [Fact]
    public void DetailPersonnageModal_DisplaysNiveau()
    {
        // Arrange
        var personnage = CreateTestPersonnage();

        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert
        Assert.Contains("100", cut.Markup);
    }

    [Fact]
    public void DetailPersonnageModal_HasSectionTitle()
    {
        // Arrange
        var personnage = CreateTestPersonnage();

        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert - Has section titles
        Assert.Contains("section-title", cut.Markup);
    }

    [Fact]
    public void DetailPersonnageModal_HasDetailItem()
    {
        // Arrange
        var personnage = CreateTestPersonnage();

        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Assert
        Assert.Contains("detail-item", cut.Markup);
    }

    #endregion

    #region Edit Mode Tests

    [Fact]
    public async Task DetailPersonnageModal_CanEnterEditMode()
    {
        // Arrange
        var personnage = CreateTestPersonnage();
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Act - Click edit button
        var editButton = cut.Find(".edit-button");
        await editButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Should show save and cancel buttons
        Assert.Contains("Enregistrer", cut.Markup);
        Assert.Contains("Annuler", cut.Markup);
    }

    [Fact]
    public async Task DetailPersonnageModal_CanCancelEdit()
    {
        // Arrange
        var personnage = CreateTestPersonnage();
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id));

        // Enter edit mode
        var editButton = cut.Find(".edit-button");
        await editButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Act - Click cancel button
        var cancelButton = cut.Find(".cancel-button");
        await cancelButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());

        // Assert - Should be back in read mode
        Assert.Contains("Éditer", cut.Markup);
    }

    [Fact]
    public void DetailPersonnageModal_StartInEditMode()
    {
        // Arrange
        var personnage = CreateTestPersonnage();

        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, personnage.Id)
            .Add(p => p.StartInEdit, true));

        // Assert - Should show save and cancel buttons
        Assert.Contains("Enregistrer", cut.Markup);
        Assert.Contains("Annuler", cut.Markup);
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public void DetailPersonnageModal_WithInvalidId_ShowsNotFound()
    {
        // Act
        var cut = RenderComponent<DetailPersonnageModal>(parameters => parameters
            .Add(p => p.PersonnageId, 99999));

        // Assert
        Assert.Contains("Personnage non trouvé", cut.Markup);
    }

    #endregion
}
