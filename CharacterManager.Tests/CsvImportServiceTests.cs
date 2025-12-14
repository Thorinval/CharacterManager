using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharacterManager.Tests;

public class CsvImportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly PersonnageService _personnageService;
    private readonly CsvImportService _csvImportService;

    public CsvImportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _personnageService = new PersonnageService(_context);
        _csvImportService = new CsvImportService(_personnageService, _context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task ImportCsvAsync_WithValidCsv_ShouldImportPersonnages()
    {
        // Arrange
        var csvContent = @"Personnage;Rareté;Type;Puissance;PA;PV;Action;Role;Niveau;Rang;Selection;Faction
REGINA;SSR;Mercenaire;3320;140;509;Mêlée;Sentinelle;14;2;Oui;Syndicat
BELLE;SSR;Mercenaire;3090;143;330;Distance;Sentinelle;8;3;Oui;Syndicat";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _csvImportService.ImportCsvAsync(stream);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.SuccessCount);
        Assert.Null(result.Error);
        
        var personnages = _personnageService.GetAll();
        Assert.Equal(2, personnages.Count());
        
        var regina = personnages.FirstOrDefault(p => p.Nom == "REGINA");
        Assert.NotNull(regina);
        Assert.Equal(Rarete.SSR, regina.Rarete);
        Assert.Equal(TypePersonnage.Mercenaire, regina.Type);
        Assert.Equal(3320, regina.Puissance);
    }

    [Fact]
    public async Task ImportCsvAsync_WithExistingPersonnage_ShouldUpdate()
    {
        // Arrange - Create an existing personnage
        var existingPersonnage = new Personnage
        {
            Nom = "REGINA",
            Rarete = Rarete.SR,
            Type = TypePersonnage.Androïde,
            Puissance = 1000,
            Niveau = 1,
            Rang = 1,
            PA = 50,
            PV = 100,
            Role = Role.Sentinelle,
            Faction = Faction.Syndicat,
            ImageUrlDetail = "old-url.jpg",
            ImageUrlPreview = "old-url_small_portrait.png",
            ImageUrlSelected = "old-url_small_select.png",
            Description = "Old description",
            Selectionne = false
        };

        _personnageService.Add(existingPersonnage);

        var csvContent = @"Personnage;Rareté;Type;Puissance;PA;PV;Action;Role;Niveau;Rang;Selection;Faction
REGINA;SSR;Mercenaire;3320;140;509;Mêlée;Sentinelle;14;2;Oui;Syndicat";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _csvImportService.ImportCsvAsync(stream);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.SuccessCount);
        
        var personnages = _personnageService.GetAll();
        Assert.Single(personnages); // Still only one personnage
        
        var updated = personnages.First();
        Assert.Equal("REGINA", updated.Nom);
        Assert.Equal(Rarete.SSR, updated.Rarete);
        Assert.Equal(TypePersonnage.Mercenaire, updated.Type);
        Assert.Equal(3320, updated.Puissance);
        Assert.Equal(140, updated.PA);
        Assert.Equal(509, updated.PV);
        Assert.True(updated.Selectionne);
    }

    [Fact]
    public async Task ImportCsvAsync_WithEmptyFile_ShouldReturnError()
    {
        // Arrange
        var csvContent = @"Personnage;Rareté;Type;Puissance;PA;PV;Action;Role;Niveau;Rang;Selection;Faction";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _csvImportService.ImportCsvAsync(stream);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task ImportCsvAsync_WithInvalidData_ShouldSkipAndContinue()
    {
        // Arrange
        var csvContent = @"Personnage;Rareté;Type;Puissance;PA;PV;Action;Role;Niveau;Rang;Selection;Faction
REGINA;SSR;Mercenaire;3320;140;509;Mêlée;Sentinelle;14;2;Oui;Syndicat
;invalid;;;;;;;;;;;
BELLE;SSR;Mercenaire;3090;143;330;Distance;Sentinelle;8;3;Oui;Syndicat";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _csvImportService.ImportCsvAsync(stream);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.SuccessCount); // Only valid rows
        
        var personnages = _personnageService.GetAll();
        Assert.Equal(2, personnages.Count());
    }

    [Fact]
    public async Task ImportCsvAsync_WithDifferentFactions_ShouldMapCorrectly()
    {
        // Arrange
        var csvContent = @"Personnage;Rareté;Type;Puissance;PA;PV;Action;Role;Niveau;Rang;Selection;Faction
REGINA;SSR;Mercenaire;3320;140;509;Mêlée;Sentinelle;14;2;Oui;Syndicat
RAVENNA;SSR;Mercenaire;3160;140;471;Mêlée;Sentinelle;18;3;Oui;Hommes libres
VICTORIA;SSR;Mercenaire;2715;173;384;Mêlée;Sentinelle;13;3;Non;Pacificateurs";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _csvImportService.ImportCsvAsync(stream);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.SuccessCount);
        
        var personnages = _personnageService.GetAll().ToList();
        Assert.Equal(Faction.Syndicat, personnages[0].Faction);
        Assert.Equal(Faction.HommesLibres, personnages[1].Faction);
        Assert.Equal(Faction.Pacificateurs, personnages[2].Faction);
    }

    [Fact]
    public async Task ImportCsvAsync_WithAndroidCharacters_ShouldImportCorrectly()
    {
        // Arrange
        var csvContent = @"Personnage;Rareté;Type;Puissance;PA;PV;Action;Role;Niveau;Rang;Selection;Faction
ISABELLA;SSR;Androïde;835;;20;Androïde;Androïde;2;;Oui;Androïde";

        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await _csvImportService.ImportCsvAsync(stream);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.SuccessCount);
        
        var android = _personnageService.GetAll().First();
        Assert.Equal("ISABELLA", android.Nom);
        Assert.Equal(TypePersonnage.Androïde, android.Type);
        Assert.Equal(Rarete.SSR, android.Rarete);
    }
}
