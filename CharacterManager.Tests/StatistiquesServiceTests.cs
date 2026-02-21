using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CharacterManager.Tests;

public class StatistiquesServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PersonnageService _personnageService;
    private readonly StatistiquesService _service;

    public StatistiquesServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        var historiqueService = new HistoriqueModificationService(_context);
        var loggerMock = new Mock<ILogger<PersonnageService>>();
        _personnageService = new PersonnageService(_context, historiqueService, loggerMock.Object);
        _service = new StatistiquesService(_context, _personnageService);
    }

    #region FormatDateWithDay Tests

    [Fact]
    public void FormatDateWithDay_ShouldFormatCorrectly()
    {
        // Arrange
        var date = new DateTime(2025, 3, 15);

        // Act
        var result = StatistiquesService.FormatDateWithDay(date);

        // Assert
        Assert.Equal("15 MAR", result);
    }

    [Fact]
    public void FormatDateWithDay_ShouldPadDayWithZero()
    {
        // Arrange
        var date = new DateTime(2025, 1, 5);

        // Act
        var result = StatistiquesService.FormatDateWithDay(date);

        // Assert
        Assert.Equal("05 JAN", result);
    }

    #endregion

    #region FormatDateForClassement Tests

    [Fact]
    public void FormatDateForClassement_ShouldFormatCorrectly()
    {
        // Arrange
        var date = new DateOnly(2025, 12, 25);

        // Act
        var result = StatistiquesService.FormatDateForClassement(date);

        // Assert
        Assert.Equal("25 DEC", result);
    }

    #endregion

    #region GetClassementTypeLabel Tests

    [Theory]
    [InlineData(TypeClassement.Nutaku, "classementNutaku")]
    [InlineData(TypeClassement.Top150, "classementTop150")]
    [InlineData(TypeClassement.France, "classementFrance")]
    public void GetClassementTypeLabel_ShouldReturnLocalizedLabel(TypeClassement type, string expectedKey)
    {
        // Arrange
        string Localize(string key) => key.Split('.').Last();

        // Act
        var result = StatistiquesService.GetClassementTypeLabel(type, Localize);

        // Assert
        Assert.Equal(expectedKey, result);
    }

    #endregion

    #region GenerateColors Tests

    [Fact]
    public void GenerateColors_ShouldReturnRequestedCount()
    {
        // Act
        var colors = StatistiquesService.GenerateColors(5);

        // Assert
        Assert.Equal(5, colors.Count);
    }

    [Fact]
    public void GenerateColors_ShouldCycleWhenExceedsBase()
    {
        // Act
        var colors = StatistiquesService.GenerateColors(20);

        // Assert
        Assert.Equal(20, colors.Count);
        Assert.Equal(colors[0], colors[16]); // cycle starts at index 16
    }

    #endregion

    #region ColorWithAlpha Tests

    [Fact]
    public void ColorWithAlpha_ShouldConvertHexToRgba()
    {
        // Arrange
        var hex = "#667eea";

        // Act
        var result = StatistiquesService.ColorWithAlpha(hex, 0.5);

        // Assert
        // French locale uses comma as decimal separator
        Assert.Contains("rgba(102,126,234,", result);
        Assert.Contains("5", result);
    }

    [Fact]
    public void ColorWithAlpha_ShouldReturnOriginalIfInvalidHex()
    {
        // Arrange
        var invalid = "#abc";

        // Act
        var result = StatistiquesService.ColorWithAlpha(invalid, 0.5);

        // Assert
        Assert.Equal("#abc", result);
    }

    #endregion

    #region GetPersonnagesWithHistory Tests

    [Fact]
    public void GetPersonnagesWithHistory_ShouldReturnOnlyThoseWithData()
    {
        // Arrange
        var dailyData = new List<LevelEvolutionData>
        {
            new() { Date = DateTime.Now.AddDays(-1), LevelsByPersonnage = new() { ["ALICE"] = 10, ["BOB"] = 0 } },
            new() { Date = DateTime.Now, LevelsByPersonnage = new() { ["ALICE"] = 12 } }
        };

        // Act
        var result = StatistiquesService.GetPersonnagesWithHistory(dailyData);

        // Assert
        Assert.Single(result);
        Assert.Equal("ALICE", result[0]);
    }

    [Fact]
    public void GetPersonnagesWithHistory_ShouldReturnEmptyWhenNoData()
    {
        // Arrange
        var dailyData = new List<LevelEvolutionData>();

        // Act
        var result = StatistiquesService.GetPersonnagesWithHistory(dailyData);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region CreateChartDatasets Tests

    [Fact]
    public void CreateChartDatasets_ShouldCreateDatasetsWithCorrectStructure()
    {
        // Arrange
        var dailyData = new List<LevelEvolutionData>
        {
            new() { Date = DateTime.Now.AddDays(-1), LevelsByPersonnage = new() { ["ALICE"] = 10 } },
            new() { Date = DateTime.Now, LevelsByPersonnage = new() { ["ALICE"] = 15 } }
        };
        var personnages = new List<string> { "ALICE" };

        // Act
        var datasets = StatistiquesService.CreateChartDatasets(dailyData, personnages, out int minLevel);

        // Assert
        Assert.Single(datasets);
        Assert.Equal(10, minLevel);
    }

    [Fact]
    public void CreateChartDatasets_ShouldUseNullForMissingData()
    {
        // Arrange
        var dailyData = new List<LevelEvolutionData>
        {
            new() { Date = DateTime.Now.AddDays(-2), LevelsByPersonnage = new() { ["ALICE"] = 10 } },
            new() { Date = DateTime.Now.AddDays(-1), LevelsByPersonnage = new() },
            new() { Date = DateTime.Now, LevelsByPersonnage = new() { ["ALICE"] = 15 } }
        };
        var personnages = new List<string> { "ALICE" };

        // Act
        var datasets = StatistiquesService.CreateChartDatasets(dailyData, personnages, out int minLevel);

        // Assert
        Assert.Single(datasets);
        Assert.Equal(10, minLevel);
    }

    #endregion

    #region GetLevelEvolutionData Tests

    [Fact]
    public void GetLevelEvolutionData_ShouldReturnEmptyWhenNoMercenaires()
    {
        // Act
        var result = _service.GetLevelEvolutionData();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLevelEvolutionData_ShouldReturnEmptyWhenNoHistory()
    {
        // Arrange
        _context.Personnages.Add(new Personnage { Nom = "ALICE", Type = TypePersonnage.Mercenaire });
        await _context.SaveChangesAsync();

        // Act
        var result = _service.GetLevelEvolutionData();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLevelEvolutionData_ShouldGroupByDay()
    {
        // Arrange
        var mercenaire = new Personnage { Nom = "ALICE", Type = TypePersonnage.Mercenaire };
        _context.Personnages.Add(mercenaire);
        await _context.SaveChangesAsync();

        _context.HistoriquesModifications.AddRange(
            new HistoriqueModification
            {
                TypeEntite = TypeEntite.Personnage,
                EntiteId = mercenaire.Id,
                NomEntite = "ALICE",
                ChampModifie = StatisticsConstants.HistoryFields.Niveau,
                NouvelleValeur = "10",
                DateModification = new DateTime(2025, 1, 15, 10, 0, 0)
            },
            new HistoriqueModification
            {
                TypeEntite = TypeEntite.Personnage,
                EntiteId = mercenaire.Id,
                NomEntite = "ALICE",
                ChampModifie = StatisticsConstants.HistoryFields.Niveau,
                NouvelleValeur = "12",
                DateModification = new DateTime(2025, 1, 15, 14, 0, 0)
            });
        await _context.SaveChangesAsync();

        // Act
        var result = _service.GetLevelEvolutionData();

        // Assert
        Assert.Single(result);
        Assert.Equal(new DateTime(2025, 1, 15).Date, result[0].Date);
        Assert.Equal(12, result[0].LevelsByPersonnage["ALICE"]);
    }

    #endregion

    #region GetClassementEvolutionData Tests

    [Fact]
    public void GetClassementEvolutionData_ShouldReturnEmptyWhenNoHistory()
    {
        // Act
        var result = _service.GetClassementEvolutionData();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClassementEvolutionData_ShouldReturnOrderedEntries()
    {
        // Arrange
        _context.HistoriquesClassement.AddRange(
            new HistoriqueClassement
            {
                DateEnregistrement = new DateOnly(2025, 1, 20),
                PuissanceTotale = 5000,
                Classements = new List<Classement>
                {
                    new() { Type = TypeClassement.Nutaku, Valeur = 100 }
                }
            },
            new HistoriqueClassement
            {
                DateEnregistrement = new DateOnly(2025, 1, 10),
                PuissanceTotale = 4000,
                Classements = new List<Classement>
                {
                    new() { Type = TypeClassement.France, Valeur = 50 }
                }
            });
        await _context.SaveChangesAsync();

        // Act
        var result = _service.GetClassementEvolutionData();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2025, 1, 10), result[0].Date);
        Assert.Equal(new DateOnly(2025, 1, 20), result[1].Date);
    }

    #endregion

    #region GetSelectedTeamPowerEvolutionData Tests

    [Fact]
    public void GetSelectedTeamPowerEvolutionData_ShouldReturnEmptyWhenNoClassement()
    {
        // Act
        var result = _service.GetSelectedTeamPowerEvolutionData();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region GetBestTeamPowerEvolutionData Tests

    [Fact]
    public void GetBestTeamPowerEvolutionData_ShouldReturnEmptyWhenNoClassement()
    {
        // Act
        var result = _service.GetBestTeamPowerEvolutionData();

        // Assert
        Assert.Empty(result);
    }

    #endregion

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _context?.Dispose();
        }
    }
}
