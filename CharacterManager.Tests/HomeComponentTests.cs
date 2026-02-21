using Bunit;
using CharacterManager.Components.Pages;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using FluentAssertions;

namespace CharacterManager.Tests;

public class HomeComponentTests : TestContext
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPersonnageService> _personnageServiceMock;
    private readonly Mock<IAdultModeNotificationService> _adultModeNotificationMock;
    private readonly Mock<IProfileService> _profileServiceMock;
    private readonly Mock<IPmlImportService> _pmlImportServiceMock;
    private readonly Mock<IPmlExportService> _pmlExportServiceMock;
    private readonly Mock<IHistoriqueLigueService> _historiqueLigueServiceMock;
    private readonly Mock<IHistoriqueClassementService> _historiqueClassementServiceMock;
    private readonly Mock<ICapaciteService> _capaciteServiceMock;
    private readonly Mock<IClientLocalizationService> _localizationServiceMock;
    private readonly Mock<IAppVersionService> _appVersionServiceMock;

    public HomeComponentTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        // Create mocks
        _personnageServiceMock = new Mock<IPersonnageService>();
        _adultModeNotificationMock = new Mock<IAdultModeNotificationService>();
        _profileServiceMock = new Mock<IProfileService>();
        _pmlImportServiceMock = new Mock<IPmlImportService>();
        _pmlExportServiceMock = new Mock<IPmlExportService>();
        _historiqueLigueServiceMock = new Mock<IHistoriqueLigueService>();
        _historiqueClassementServiceMock = new Mock<IHistoriqueClassementService>();
        _capaciteServiceMock = new Mock<ICapaciteService>();
        _localizationServiceMock = new Mock<IClientLocalizationService>();
        _appVersionServiceMock = new Mock<IAppVersionService>();

        // Setup localization
        SetupLocalizationMocks();

        // Register services in the test context
        Services.AddScoped<IPersonnageService>(_ => _personnageServiceMock.Object);
        Services.AddScoped<IAdultModeNotificationService>(_ => _adultModeNotificationMock.Object);
        Services.AddScoped<ApplicationDbContext>(_ => _context);
        Services.AddScoped<IProfileService>(_ => _profileServiceMock.Object);
        Services.AddScoped<IHttpContextAccessor>(_ => new HttpContextAccessor());
        Services.AddScoped<IPmlImportService>(_ => _pmlImportServiceMock.Object);
        Services.AddScoped<IPmlExportService>(_ => _pmlExportServiceMock.Object);
        Services.AddScoped<IHistoriqueLigueService>(_ => _historiqueLigueServiceMock.Object);
        Services.AddScoped<IHistoriqueClassementService>(_ => _historiqueClassementServiceMock.Object);
        Services.AddScoped<ICapaciteService>(_ => _capaciteServiceMock.Object);
        Services.AddScoped<IClientLocalizationService>(_ => _localizationServiceMock.Object);
        Services.AddScoped<IAppVersionService>(_ => _appVersionServiceMock.Object);
        Services.AddScoped<IHistoriqueModificationService>(_ => new HistoriqueModificationService(_context));
    }

    private void SetupLocalizationMocks()
    {
        _localizationServiceMock.Setup(l => l.GetKeyValue("navigation.home")).Returns("Accueil");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.welcome")).Returns("Bienvenue");
        _localizationServiceMock.Setup(l => l.GetKeyValue("navigation.squad")).Returns("Escouade");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.squad")).Returns("Gérez votre escouade");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.mercByFaction")).Returns("Mercs par faction");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.mercByAttack")).Returns("Mercs par attaque");
        _localizationServiceMock.Setup(l => l.GetKeyValue("navigation.statistics")).Returns("Statistiques");
        _localizationServiceMock.Setup(l => l.GetKeyValue("navigation.importExportPml")).Returns("Import/Export");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.pml")).Returns("Gérez vos fichiers PML");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.highestLeagueNone")).Returns("Aucune");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.eliteTop50")).Returns("Elite Top 50");
        _localizationServiceMock.Setup(l => l.GetKeyValue("leagueHistory.table.league")).Returns("Ligue");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.faction.syndicat")).Returns("Syndicat");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.faction.pacificateurs")).Returns("Pacificateurs");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.faction.hommesLibres")).Returns("Hommes Libres");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.faction.inconnu")).Returns("Inconnu");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.attackType.melee")).Returns("Mêlée");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.attackType.distance")).Returns("Distance");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.attackType.android")).Returns("Androïde");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.attackType.commander")).Returns("Commandant");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.attackType.unknown")).Returns("Inconnu");
        _localizationServiceMock.Setup(l => l.GetCurrentLanguage()).Returns("fr-FR");
    }

    private void SetupDefaultMockBehaviors()
    {
        _personnageServiceMock.Setup(x => x.GetPuissanceEscouade()).Returns(1000);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxEscouade()).Returns(1500);
        _personnageServiceMock.Setup(x => x.GetPuissanceLucieEscouade()).Returns(500);
        _personnageServiceMock.Setup(x => x.GetMercenairesAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetInventoryCounts())
            .Returns((0, 0, 0));
        _personnageServiceMock.Setup(x => x.GetTopMercenairesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetTopAndroidesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetTopCommandantAsync())
            .ReturnsAsync((Personnage?)null);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxLucieEscouade()).Returns(800);

        _historiqueLigueServiceMock.Setup(x => x.GetHighestLeagueAsync())
            .ReturnsAsync((int?)null);
        _pmlImportServiceMock.Setup(x => x.GetLastImportedDateAsync())
            .ReturnsAsync((DateTime?)null);
        _pmlExportServiceMock.Setup(x => x.GetLastExportDate())
            .ReturnsAsync((DateTime?)null);
        _pmlImportServiceMock.Setup(x => x.GetLastImportedFileName())
            .ReturnsAsync((string?)null);
        _historiqueClassementServiceMock.Setup(x => x.GetHistoriqueRecentAsync(1))
            .ReturnsAsync(new List<HistoriqueClassement>());
        _capaciteServiceMock.Setup(x => x.GetCount()).Returns(0);
    }

    [Fact]
    public async Task Home_ShouldRenderSuccessfully()
    {
        // Arrange
        SetupDefaultMockBehaviors();

        // Act
        var cut = RenderComponent<Home>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        Assert.NotNull(cut);
        cut.Markup.Should().Contain("Character Manager");
    }

    [Fact]
    public async Task Home_ShouldDisplayHubContainer()
    {
        // Arrange
        SetupDefaultMockBehaviors();

        // Act
        var cut = RenderComponent<Home>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Find(".hub-container").Should().NotBeNull();
    }

    [Fact]
    public async Task Home_ShouldDisplaySquadSection()
    {
        // Arrange
        SetupDefaultMockBehaviors();

        // Act
        var cut = RenderComponent<Home>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("Gestion des Escouades");
    }

    [Fact]
    public async Task Home_ShouldDisplayPowerValues()
    {
        // Arrange
        SetupDefaultMockBehaviors();

        // Act
        var cut = RenderComponent<Home>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("1000"); // puissanceEscouade
    }

    [Fact]
    public async Task Home_ShouldDisplayImportExportSection()
    {
        // Arrange
        SetupDefaultMockBehaviors();

        // Act
        var cut = RenderComponent<Home>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("Import / Export");
    }

    [Fact]
    public async Task Home_WithNoData_ShouldShowPmlImportAlert()
    {
        // Arrange
        _personnageServiceMock.Setup(x => x.GetPuissanceEscouade()).Returns(0);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxEscouade()).Returns(0);
        _personnageServiceMock.Setup(x => x.GetPuissanceLucieEscouade()).Returns(0);
        _personnageServiceMock.Setup(x => x.GetMercenairesAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetInventoryCounts())
            .Returns((0, 0, 0));
        _personnageServiceMock.Setup(x => x.GetTopMercenairesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetTopAndroidesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetTopCommandantAsync())
            .ReturnsAsync((Personnage?)null);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxLucieEscouade()).Returns(0);
        _historiqueLigueServiceMock.Setup(x => x.GetHighestLeagueAsync())
            .ReturnsAsync((int?)null);
        _pmlImportServiceMock.Setup(x => x.GetLastImportedDateAsync())
            .ReturnsAsync((DateTime?)null);
        _pmlExportServiceMock.Setup(x => x.GetLastExportDate())
            .ReturnsAsync((DateTime?)null);
        _pmlImportServiceMock.Setup(x => x.GetLastImportedFileName())
            .ReturnsAsync((string?)null);
        _historiqueClassementServiceMock.Setup(x => x.GetHistoriqueRecentAsync(1))
            .ReturnsAsync(new List<HistoriqueClassement>());
        _capaciteServiceMock.Setup(x => x.GetCount()).Returns(0);

        // Act
        var cut = RenderComponent<Home>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("alert");
    }

    [Fact]
    public async Task Home_ShouldDisplayMercenairesMetrics()
    {
        // Arrange
        var mercenaires = new List<Personnage>
        {
            new() { Id = 1, Nom = "Merc1", Faction = Faction.Syndicat, Selectionne = true, TypeAttaque = TypeAttaque.Melee, Puissance = 100 },
            new() { Id = 2, Nom = "Merc2", Faction = Faction.Syndicat, Selectionne = true, TypeAttaque = TypeAttaque.Distance, Puissance = 150 },
            new() { Id = 3, Nom = "Merc3", Faction = Faction.Pacificateurs, Selectionne = true, TypeAttaque = TypeAttaque.Melee, Puissance = 120 }
        };

        _personnageServiceMock.Setup(x => x.GetPuissanceEscouade()).Returns(370);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxEscouade()).Returns(500);
        _personnageServiceMock.Setup(x => x.GetPuissanceLucieEscouade()).Returns(100);
        _personnageServiceMock.Setup(x => x.GetMercenairesAsync(true))
            .ReturnsAsync(mercenaires);
        _personnageServiceMock.Setup(x => x.GetMercenairesAsync(false))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetInventoryCounts())
            .Returns((0, 3, 0));
        _personnageServiceMock.Setup(x => x.GetTopMercenairesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetTopAndroidesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetTopCommandantAsync())
            .ReturnsAsync((Personnage?)null);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxLucieEscouade()).Returns(100);
        _historiqueLigueServiceMock.Setup(x => x.GetHighestLeagueAsync())
            .ReturnsAsync((int?)null);
        _pmlImportServiceMock.Setup(x => x.GetLastImportedDateAsync())
            .ReturnsAsync((DateTime?)null);
        _pmlExportServiceMock.Setup(x => x.GetLastExportDate())
            .ReturnsAsync((DateTime?)null);
        _pmlImportServiceMock.Setup(x => x.GetLastImportedFileName())
            .ReturnsAsync((string?)null);
        _historiqueClassementServiceMock.Setup(x => x.GetHistoriqueRecentAsync(1))
            .ReturnsAsync(new List<HistoriqueClassement>());
        _capaciteServiceMock.Setup(x => x.GetCount()).Returns(0);

        // Act
        var cut = RenderComponent<Home>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("370");
    }

    [Fact]
    public async Task Home_ShouldDisplayLucieData()
    {
        // Arrange
        var lucieHouse = new LucieHouse { Affection = 100 };
        var piece = new Piece
        {
            Nom = "Salle",
            Niveau = 3,
            Selectionnee = true,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 100, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 50, Bonus = new() }
        };
        lucieHouse.Pieces.Add(piece);
        
        _context.LucieHouses.Add(lucieHouse);
        await _context.SaveChangesAsync();

        SetupDefaultMockBehaviors();

        // Act
        var cut = RenderComponent<Home>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("lucie-pieces-chips");
        cut.Markup.Should().Contain("Salle");
        cut.Markup.Should().Contain("Niv. 3");
    }

    [Fact]
    public async Task Home_ShouldDisplayStatisticsCard()
    {
        // Arrange
        SetupDefaultMockBehaviors();

        // Act
        var cut = RenderComponent<Home>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("Statistiques");
    }

    [Fact]
    public async Task Home_CardsShouldBeClickable()
    {
        // Arrange
        SetupDefaultMockBehaviors();

        // Act
        var cut = RenderComponent<Home>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        var cards = cut.FindAll(".hub-card");
        Assert.NotEmpty(cards);
    }

    [Fact]
    public async Task Home_ShouldDisplayInventoryCount()
    {
        // Arrange
        _personnageServiceMock.Setup(x => x.GetPuissanceEscouade()).Returns(1000);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxEscouade()).Returns(1500);
        _personnageServiceMock.Setup(x => x.GetPuissanceLucieEscouade()).Returns(500);
        _personnageServiceMock.Setup(x => x.GetMercenairesAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetInventoryCounts())
            .Returns((1, 5, 2));
        _personnageServiceMock.Setup(x => x.GetTopMercenairesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetTopAndroidesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetTopCommandantAsync())
            .ReturnsAsync((Personnage?)null);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxLucieEscouade()).Returns(800);
        _historiqueLigueServiceMock.Setup(x => x.GetHighestLeagueAsync())
            .ReturnsAsync((int?)null);
        _pmlImportServiceMock.Setup(x => x.GetLastImportedDateAsync())
            .ReturnsAsync((DateTime?)null);
        _pmlExportServiceMock.Setup(x => x.GetLastExportDate())
            .ReturnsAsync((DateTime?)null);
        _pmlImportServiceMock.Setup(x => x.GetLastImportedFileName())
            .ReturnsAsync((string?)null);
        _historiqueClassementServiceMock.Setup(x => x.GetHistoriqueRecentAsync(1))
            .ReturnsAsync(new List<HistoriqueClassement>());
        _capaciteServiceMock.Setup(x => x.GetCount()).Returns(0);

        // Act
        var cut = RenderComponent<Home>();
        await cut.InvokeAsync(() => Task.CompletedTask);

        // Assert
        cut.Markup.Should().Contain("Inventaire");
    }
}
