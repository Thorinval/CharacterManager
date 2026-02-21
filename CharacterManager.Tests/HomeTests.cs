using CharacterManager.Components.Pages;
using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Moq;
using Xunit;
using System.Security.Claims;

namespace CharacterManager.Tests;

public class HomeTests : IDisposable
{
    private bool _disposed;
    private readonly ApplicationDbContext _context;
    private readonly Mock<IPersonnageService> _personnageServiceMock;
    private readonly Mock<IAdultModeNotificationService> _adultModeNotificationMock;
    private readonly Mock<IProfileService> _profileServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IPmlImportService> _pmlImportServiceMock;
    private readonly Mock<IPmlExportService> _pmlExportServiceMock;
    private readonly Mock<IHistoriqueLigueService> _historiqueLigueServiceMock;
    private readonly Mock<IHistoriqueClassementService> _historiqueClassementServiceMock;
    private readonly Mock<ICapaciteService> _capaciteServiceMock;
    private readonly Mock<IClientLocalizationService> _localizationServiceMock;
    private readonly Home _home;

    public HomeTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();

        // Create all mocks using interfaces
        _personnageServiceMock = new Mock<IPersonnageService>();
        _adultModeNotificationMock = new Mock<IAdultModeNotificationService>();
        _profileServiceMock = new Mock<IProfileService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _pmlImportServiceMock = new Mock<IPmlImportService>();
        _pmlExportServiceMock = new Mock<IPmlExportService>();
        _historiqueLigueServiceMock = new Mock<IHistoriqueLigueService>();
        _historiqueClassementServiceMock = new Mock<IHistoriqueClassementService>();
        _capaciteServiceMock = new Mock<ICapaciteService>();
        _localizationServiceMock = new Mock<IClientLocalizationService>();
        
        // Setup default mock behaviors for localization
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.faction.syndicat")).Returns("Syndicat");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.faction.pacificateurs")).Returns("Pacificateurs");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.faction.hommesLibres")).Returns("Hommes Libres");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.faction.inconnu")).Returns("Inconnu");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.attackType.melee")).Returns("Mêlée");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.attackType.distance")).Returns("Distance");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.attackType.android")).Returns("Androïde");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.attackType.commander")).Returns("Commandant");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.attackType.unknown")).Returns("Inconnu");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.highestLeagueNone")).Returns("Aucune");
        _localizationServiceMock.Setup(l => l.GetKeyValue("home.eliteTop50")).Returns("Elite Top 50");
        _localizationServiceMock.Setup(l => l.GetKeyValue("leagueHistory.table.league")).Returns("Ligue");

        // Create Home instance (concrete class) and inject mocked dependencies
        _home = new Home
        {
            PersonnageService = _personnageServiceMock.Object,
            AdultModeNotification = _adultModeNotificationMock.Object,
            DbContext = _context,
            ProfileService = _profileServiceMock.Object,
            HttpContextAccessor = _httpContextAccessorMock.Object,
            PmlImportService = _pmlImportServiceMock.Object,
            PmlExportService = _pmlExportServiceMock.Object,
            HistoriqueLigueService = _historiqueLigueServiceMock.Object,
            HistoriqueClassementService = _historiqueClassementServiceMock.Object,
            CapaciteService = _capaciteServiceMock.Object
        };

        // Set LocalizationService via reflection (if it's a protected/private property)
        var homeType = typeof(Home);
        var localizationProperty = homeType.GetProperty("LocalizationService", 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Instance);
        if (localizationProperty != null)
        {
            localizationProperty.SetValue(_home, _localizationServiceMock.Object);
        }
    }

    // Helper method to call protected OnInitializedAsync
    private async Task CallOnInitializedAsync()
    {
        var method = typeof(Home).GetMethod("OnInitializedAsync", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (method != null)
        {
            var task = method.Invoke(_home, null) as Task;
            if (task != null)
            {
                await task;
            }
        }
    }

    #region OnInitializedAsync Tests

    [Fact]
    public async Task OnInitializedAsync_ShouldSubscribeToAdultModeNotification()
    {
        // Arrange
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

        // Act
        await CallOnInitializedAsync();

        // Assert
        _adultModeNotificationMock.Verify(x => x.Subscribe(It.IsAny<Action<bool>>()), Times.Once);
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldLoadPuissanceValues()
    {
        // Arrange
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

        // Act
        await CallOnInitializedAsync();

        // Assert
        Assert.Equal(1000, _home.puissanceEscouade);
        Assert.Equal(1500, _home.puissanceMeilleureEscouade);
        Assert.Equal(500, _home.puissanceLucieEscouade);
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldLoadMercenairesParFaction()
    {
        // Arrange
        var mercenaires = new List<Personnage>
        {
            new() { Id = 1, Nom = "Merc1", Faction = Faction.Syndicat, Selectionne = true, TypeAttaque = TypeAttaque.Melee },
            new() { Id = 2, Nom = "Merc2", Faction = Faction.Syndicat, Selectionne = true, TypeAttaque = TypeAttaque.Distance },
            new() { Id = 3, Nom = "Merc3", Faction = Faction.Pacificateurs, Selectionne = true, TypeAttaque = TypeAttaque.Melee }
        };

        _personnageServiceMock.Setup(x => x.GetPuissanceEscouade()).Returns(1000);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxEscouade()).Returns(1500);
        _personnageServiceMock.Setup(x => x.GetPuissanceLucieEscouade()).Returns(500);
        _personnageServiceMock.Setup(x => x.GetMercenairesAsync(true))
            .ReturnsAsync(mercenaires);
        _personnageServiceMock.Setup(x => x.GetMercenairesAsync(false))
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

        // Act
        await CallOnInitializedAsync();

        // Assert
        Assert.Equal(2, _home.mercenairesParFaction[Faction.Syndicat]);
        Assert.Equal(1, _home.mercenairesParFaction[Faction.Pacificateurs]);
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldLoadLucieHouseData()
    {
        // Arrange
        var lucieHouse = new LucieHouse { Affection = 100 };
        
        var piece1 = new Piece 
        { 
            Nom = "Salle", 
            Niveau = 3,
            Selectionnee = true,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 100, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 50, Bonus = new() }
        };
        
        var piece2 = new Piece 
        { 
            Nom = "Cuisine", 
            Niveau = 2,
            Selectionnee = false,
            BonusTactiquesSerialized = "[]",
            BonusStrategiquesSerialized = "[]",
            AspectsTactiques = new() { Nom = "Tactiques", Puissance = 80, Bonus = new() },
            AspectsStrategiques = new() { Nom = "Strategiques", Puissance = 40, Bonus = new() }
        };

        lucieHouse.Pieces.Add(piece1);
        lucieHouse.Pieces.Add(piece2);
        
        _context.LucieHouses.Add(lucieHouse);
        await _context.SaveChangesAsync();

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

        // Act
        await CallOnInitializedAsync();

        // Assert
        Assert.Equal(100, _home.lucieAffection);
        Assert.Equal(2, _home.luciePieces.Count);
        Assert.Equal("Salle", _home.luciePieces[0].Nom);
        Assert.Equal(150, _home.luciePieces[0].Puissance);
    }

    [Fact]
    public async Task OnInitializedAsync_WithEmptyDatabase_ShouldShowPmlImportAlert()
    {
        // Arrange - empty database
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
        await CallOnInitializedAsync();

        // Assert
        Assert.True(_home.showPmlImportAlert);
        Assert.NotNull(_home.importError);
    }

    #endregion

    #region Helper Methods Tests

    [Theory]
    [InlineData(Faction.Syndicat, "shape-triangle")]
    [InlineData(Faction.Pacificateurs, "shape-square")]
    [InlineData(Faction.HommesLibres, "shape-circle")]
    public void GetFactionShapeClass_ShouldReturnCorrectClass(Faction faction, string expected)
    {
        // Act
        var result = Home.GetFactionShapeClass(faction);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(Faction.Syndicat, "faction-syndicat")]
    [InlineData(Faction.Pacificateurs, "faction-pacificateurs")]
    [InlineData(Faction.HommesLibres, "faction-hommeslibres")]
    public void GetFactionColorClass_ShouldReturnCorrectClass(Faction faction, string expected)
    {
        // Act
        var result = Home.GetFactionColorClass(faction);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(TypeAttaque.Melee, "bi-hand-thumbs-up-fill")]
    [InlineData(TypeAttaque.Distance, "bi-bullseye")]
    [InlineData(TypeAttaque.Androide, "bi-cpu")]
    [InlineData(TypeAttaque.Commandant, "bi-star-fill")]
    public void GetTypeAttaqueIcon_ShouldReturnCorrectIcon(TypeAttaque type, string expected)
    {
        // Act
        var result = Home.GetTypeAttaqueIcon(type);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, "-")]
    [InlineData(1, "1")]
    [InlineData(100, "100")]
    public void FormatClassementValeur_ShouldFormatCorrectly(int valeur, string expected)
    {
        // Act
        var result = Home.FormatClassementValeur(valeur);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatDate_WithNullValue_ShouldReturnDash()
    {
        // Act
        var result = _home.FormatDate(null);

        // Assert
        Assert.Equal("-", result);
    }

    [Fact]
    public void FormatDate_WithValue_ShouldFormatCorrectly()
    {
        // Arrange
        var date = new DateTime(2025, 1, 15, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = _home.FormatDate(date);

        // Assert
        Assert.NotEqual("-", result);
        Assert.Contains("15", result); // Should contain the day
    }

    [Theory]
    [InlineData(Faction.Syndicat, "Syndicat")]
    [InlineData(Faction.Pacificateurs, "Pacificateurs")]
    [InlineData(Faction.HommesLibres, "Hommes Libres")]
    public void GetFactionLabel_ShouldReturnLocalizedValueForAllFactions(Faction faction, string expected)
    {
        // Act
        var result = _home.GetFactionLabel(faction);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(TypeAttaque.Melee, "Mêlée")]
    [InlineData(TypeAttaque.Distance, "Distance")]
    [InlineData(TypeAttaque.Androide, "Androïde")]
    [InlineData(TypeAttaque.Commandant, "Commandant")]
    public void GetTypeAttaqueLabel_ShouldReturnLocalizedValueForAllTypes(TypeAttaque type, string expected)
    {
        // Act
        var result = _home.GetTypeAttaqueLabel(type);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "Aucune")]
    [InlineData(50, "Elite Top 50")]
    [InlineData(10, "Ligue 10")]
    [InlineData(1, "Ligue 1")]
    public void FormatLigueLabel_ShouldReturnCorrectLabel(int? ligue, string expected)
    {
        // Act
        var result = _home.FormatLigueLabel(ligue);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatDate_WithFrenchCulture_ShouldFormatWithFrenchDate()
    {
        // Arrange
        _localizationServiceMock.Setup(l => l.GetCurrentLanguage()).Returns("fr-FR");
        var date = new DateTime(2025, 1, 15, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = _home.FormatDate(date);

        // Assert
        Assert.Contains("15/01/2025", result);
    }

    [Fact]
    public void FormatDate_WithEnglishCulture_ShouldFormatWithEnglishDate()
    {
        // Arrange
        _localizationServiceMock.Setup(l => l.GetCurrentLanguage()).Returns("en-US");
        var date = new DateTime(2025, 1, 15, 14, 30, 0, DateTimeKind.Utc);

        // Act
        var result = _home.FormatDate(date);

        // Assert
        Assert.Contains("2025-01-15", result);
    }

    #region Adult Mode Tests - Additional scenarios

    [Fact]
    public async Task GetAdultModeFromSettingsAsync_WithEnabledSetting_ShouldReturnTrue()
    {
        // Arrange
        var settings = new AppSettings { IsAdultModeEnabled = true };
        _context.AppSettings.Add(settings);
        await _context.SaveChangesAsync();

        // Act
        var method = typeof(Home).GetMethod("GetAdultModeFromSettingsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = method?.Invoke(_home, null) as Task<bool>;
        var result = await task!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetAdultModeFromSettingsAsync_WithDisabledSetting_ShouldReturnFalse()
    {
        // Arrange
        var settings = new AppSettings { IsAdultModeEnabled = false };
        _context.AppSettings.Add(settings);
        await _context.SaveChangesAsync();

        // Act
        var method = typeof(Home).GetMethod("GetAdultModeFromSettingsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = method?.Invoke(_home, null) as Task<bool>;
        var result = await task!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAdultModeFromSettingsAsync_WithNoSettings_ShouldReturnFalse()
    {
        // Act
        var method = typeof(Home).GetMethod("GetAdultModeFromSettingsAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = method?.Invoke(_home, null) as Task<bool>;
        var result = await task!;

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAdultModeForAuthenticatedUserAsync_WithValidProfile_ShouldReturnProfileSetting()
    {
        // Arrange
        var profile = new Profile { Username = "testuser", AdultMode = true };
        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();

        var claims = new[] { new Claim(ClaimTypes.Name, "testuser") };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _profileServiceMock.Setup(x => x.GetByUsernameAsync("testuser"))
            .ReturnsAsync(profile);

        // Act
        var method = typeof(Home).GetMethod("GetAdultModeForAuthenticatedUserAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = method?.Invoke(_home, new object[] { claimsPrincipal }) as Task<bool>;
        var result = await task!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetAdultModeForAuthenticatedUserAsync_WithNullProfile_ShouldReturnSettingValue()
    {
        // Arrange
        var settings = new AppSettings { IsAdultModeEnabled = true };
        _context.AppSettings.Add(settings);
        await _context.SaveChangesAsync();

        var claims = new[] { new Claim(ClaimTypes.Name, "unknownuser") };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _profileServiceMock.Setup(x => x.GetByUsernameAsync("unknownuser"))
            .ReturnsAsync((Profile?)null);

        // Act
        var method = typeof(Home).GetMethod("GetAdultModeForAuthenticatedUserAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = method?.Invoke(_home, new object[] { claimsPrincipal }) as Task<bool>;
        var result = await task!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetCurrentAdultModeAsync_WithUnauthenticatedUser_ShouldReturnSettingValue()
    {
        // Arrange
        var settings = new AppSettings { IsAdultModeEnabled = true };
        _context.AppSettings.Add(settings);
        await _context.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        // Act
        var method = typeof(Home).GetMethod("GetCurrentAdultModeAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var task = method?.Invoke(_home, null) as Task<bool>;
        var result = await task!;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldLoadLastClassement()
    {
        // Arrange
        var classement = new HistoriqueClassement();
        classement.Classements.Add(new() { Type = TypeClassement.Nutaku, Valeur = 100 });
        classement.Classements.Add(new() { Type = TypeClassement.Top150, Valeur = 50 });
        classement.Classements.Add(new() { Type = TypeClassement.France, Valeur = 75 });
        
        _context.HistoriquesClassement.Add(classement);
        await _context.SaveChangesAsync();

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
            .ReturnsAsync(new List<HistoriqueClassement> { classement });
        _capaciteServiceMock.Setup(x => x.GetCount()).Returns(0);

        // Act
        await CallOnInitializedAsync();

        // Assert
        Assert.NotNull(_home.lastClassementSummary);
        Assert.Equal(100, _home.lastClassementSummary.Value.Nutaku);
        Assert.Equal(50, _home.lastClassementSummary.Value.Top150);
        Assert.Equal(75, _home.lastClassementSummary.Value.France);
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldLoadCapacitesCount()
    {
        // Arrange
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
        _capaciteServiceMock.Setup(x => x.GetCount()).Returns(42);

        // Act
        await CallOnInitializedAsync();

        // Assert
        Assert.Equal(42, _home.capacitesCount);
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldLoadHighestLigue()
    {
        // Arrange
        _personnageServiceMock.Setup(x => x.GetPuissanceEscouade()).Returns(1000);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxEscouade()).Returns(1500);
        _personnageServiceMock.Setup(x => x.GetPuissanceLucieEscouade()).Returns(500);
        _personnageServiceMock.Setup(x => x.GetMercenairesAsync(It.IsAny<bool>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetInventoryCounts())
            .Returns((1, 2, 3));
        _personnageServiceMock.Setup(x => x.GetTopMercenairesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetTopAndroidesAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetTopCommandantAsync())
            .ReturnsAsync((Personnage?)null);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxLucieEscouade()).Returns(800);
        
        _historiqueLigueServiceMock.Setup(x => x.GetHighestLeagueAsync())
            .ReturnsAsync(25);
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
        await CallOnInitializedAsync();

        // Assert
        Assert.Equal("Ligue 25", _home.highestLigueLabel);
    }

    [Fact]
    public async Task OnInitializedAsync_ShouldLoadImportExportDates()
    {
        // Arrange
        var importDate = new DateTime(2024, 12, 25, 10, 30, 0, DateTimeKind.Utc);
        var exportDate = new DateTime(2024, 12, 26, 14, 45, 0, DateTimeKind.Utc);

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
            .ReturnsAsync(importDate);
        _pmlExportServiceMock.Setup(x => x.GetLastExportDate())
            .ReturnsAsync(exportDate);
        _pmlImportServiceMock.Setup(x => x.GetLastImportedFileName())
            .ReturnsAsync("export_20241225.pml");
        _historiqueClassementServiceMock.Setup(x => x.GetHistoriqueRecentAsync(1))
            .ReturnsAsync(new List<HistoriqueClassement>());
        _capaciteServiceMock.Setup(x => x.GetCount()).Returns(0);

        // Act
        await CallOnInitializedAsync();

        // Assert
        Assert.NotNull(_home.lastImportDate);
        Assert.NotNull(_home.lastExportDate);
        Assert.Equal("export_20241225.pml", _home.lastImportFileName);
    }

    [Fact]
    public async Task OnInitializedAsync_WithBestSquadComposition_ShouldLoadCorrectly()
    {
        // Arrange
        var topMercenaires = new List<Personnage>
        {
            new() { Faction = Faction.Syndicat, TypeAttaque = TypeAttaque.Melee },
            new() { Faction = Faction.Pacificateurs, TypeAttaque = TypeAttaque.Distance },
        };
        var topAndroides = new List<Personnage>
        {
            new() { Faction = Faction.HommesLibres, TypeAttaque = TypeAttaque.Androide },
        };
        var topCommandant = new Personnage { Faction = Faction.Syndicat, TypeAttaque = TypeAttaque.Commandant };

        _personnageServiceMock.Setup(x => x.GetPuissanceEscouade()).Returns(1000);
        _personnageServiceMock.Setup(x => x.GetPuissanceMaxEscouade()).Returns(1500);
        _personnageServiceMock.Setup(x => x.GetPuissanceLucieEscouade()).Returns(500);
        _personnageServiceMock.Setup(x => x.GetMercenairesAsync(true))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetMercenairesAsync(false))
            .ReturnsAsync(new List<Personnage>());
        _personnageServiceMock.Setup(x => x.GetInventoryCounts())
            .Returns((0, 0, 0));
        _personnageServiceMock.Setup(x => x.GetTopMercenairesAsync(It.IsAny<int>()))
            .ReturnsAsync(topMercenaires);
        _personnageServiceMock.Setup(x => x.GetTopAndroidesAsync(It.IsAny<int>()))
            .ReturnsAsync(topAndroides);
        _personnageServiceMock.Setup(x => x.GetTopCommandantAsync())
            .ReturnsAsync(topCommandant);
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
        await CallOnInitializedAsync();

        // Assert
        Assert.Equal(2, _home.bestSquadFactions[Faction.Syndicat]);
        Assert.Equal(1, _home.bestSquadFactions[Faction.Pacificateurs]);
        Assert.Equal(1, _home.bestSquadFactions[Faction.HommesLibres]);
    }

    #endregion

    [Fact]
    public async Task OnAdultModeChanged_ShouldUpdateIsAdultModeEnabled()
    {
        // Arrange
        Action<bool>? capturedCallback = null;
        _adultModeNotificationMock.Setup(x => x.Subscribe(It.IsAny<Action<bool>>()))
            .Callback<Action<bool>>(callback => capturedCallback = callback);
        
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

        _home.isAdultModeEnabled = false;

        // Initialize the component to trigger subscription
        await CallOnInitializedAsync();

        // Act - Invoke the captured callback
        Assert.NotNull(capturedCallback);
        capturedCallback!(true);

        // Assert
        Assert.True(_home.isAdultModeEnabled);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task DisposeAsync_ShouldUnsubscribeFromAdultModeNotification()
    {
        // Arrange
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

        await CallOnInitializedAsync();

        // Act
        await ((IAsyncDisposable)_home).DisposeAsync();

        // Assert
        _adultModeNotificationMock.Verify(x => x.Unsubscribe(It.IsAny<Action<bool>>()), Times.Once);
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _context?.Dispose();
        }

        _disposed = true;
    }
}
