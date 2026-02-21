using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace CharacterManager.Tests;

public class HistoriqueClassementServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly HistoriqueClassementService _service;

    public HistoriqueClassementServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
        _service = new HistoriqueClassementService(_context);
    }

    #region GetHistoriqueAsync Tests

    [Fact]
    public async Task GetHistoriqueAsync_ShouldReturnEmptyList_WhenNoHistorique()
    {
        // Act
        var result = await _service.GetHistoriqueAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoriqueAsync_ShouldReturnOrderedByDateDescending()
    {
        // Arrange
        await CreateTestHistoriques();

        // Act
        var result = await _service.GetHistoriqueAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.True(result[0].DateEnregistrement >= result[1].DateEnregistrement);
        Assert.True(result[1].DateEnregistrement >= result[2].DateEnregistrement);
    }

    [Fact]
    public async Task GetHistoriqueAsync_WithDateRange_ShouldFilterResults()
    {
        // Arrange
        await CreateTestHistoriques();
        var dateDebut = new DateTime(2025, 1, 10, 0, 0, 0, DateTimeKind.Utc);
        var dateFin = new DateTime(2025, 1, 26, 0, 0, 0, DateTimeKind.Utc); // Include Jan 25

        // Act
        var result = await _service.GetHistoriqueAsync(dateDebut, dateFin);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region GetHistoriqueRecentAsync Tests

    [Fact]
    public async Task GetHistoriqueRecentAsync_ShouldLimitResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            _context.HistoriquesClassement.Add(CreateHistoriqueClassement(DateOnly.FromDateTime(DateTime.Now.AddDays(-i))));
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetHistoriqueRecentAsync(5);

        // Assert
        Assert.Equal(5, result.Count);
    }

    #endregion

    #region SupprimerEnregistrementAsync Tests

    [Fact]
    public async Task SupprimerEnregistrementAsync_ShouldRemoveHistorique()
    {
        // Arrange
        var historique = CreateHistoriqueClassement(DateOnly.FromDateTime(DateTime.Now));
        _context.HistoriquesClassement.Add(historique);
        await _context.SaveChangesAsync();
        var id = historique.Id;

        // Act
        await _service.SupprimerEnregistrementAsync(id);

        // Assert
        Assert.Null(await _context.HistoriquesClassement.FindAsync(id));
    }

    [Fact]
    public async Task SupprimerEnregistrementAsync_ShouldNotThrow_WhenNotFound()
    {
        // Act & Assert - should not throw
        var exception = await Record.ExceptionAsync(() => _service.SupprimerEnregistrementAsync(999));
        Assert.Null(exception);
    }

    #endregion

    #region ViderHistoriqueAsync Tests

    [Fact]
    public async Task ViderHistoriqueAsync_ShouldRemoveAllHistoriques()
    {
        // Arrange
        await CreateTestHistoriques();
        Assert.Equal(3, await _context.HistoriquesClassement.CountAsync());

        // Act
        await _service.ViderHistoriqueAsync();

        // Assert
        Assert.Empty(await _context.HistoriquesClassement.ToListAsync());
    }

    #endregion

    #region UpdateClassementEditableAsync Tests

    [Fact]
    public async Task UpdateClassementEditableAsync_ShouldUpdateFields()
    {
        // Arrange
        var historique = CreateHistoriqueClassement(new DateOnly(2025, 1, 15));
        _context.HistoriquesClassement.Add(historique);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateClassementEditableAsync(
            historique.Id,
            new DateOnly(2025, 2, 20),
            ligue: 5,
            score: 5000,
            nutaku: 100,
            top150: 50,
            france: 25);

        // Assert
        var updated = await _context.HistoriquesClassement
            .Include(h => h.Classements)
            .FirstAsync(h => h.Id == historique.Id);
        
        Assert.Equal(new DateOnly(2025, 2, 20), updated.DateEnregistrement);
        Assert.Equal(5, updated.Ligue);
        Assert.Equal(5000, updated.Score);
        Assert.Equal(100, updated.Classements.First(c => c.Type == TypeClassement.Nutaku).Valeur);
        Assert.Equal(50, updated.Classements.First(c => c.Type == TypeClassement.Top150).Valeur);
        Assert.Equal(25, updated.Classements.First(c => c.Type == TypeClassement.France).Valeur);
    }

    [Fact]
    public async Task UpdateClassementEditableAsync_ShouldThrow_WhenNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateClassementEditableAsync(999, DateOnly.FromDateTime(DateTime.Now), 1, 1000, 100, 50, 25));
    }

    [Fact]
    public async Task UpdateClassementEditableAsync_ShouldAddMissingClassements()
    {
        // Arrange - create historique without classements
        var historique = new HistoriqueClassement
        {
            DateEnregistrement = new DateOnly(2025, 1, 15),
            Ligue = 10,
            Score = 1000,
            PuissanceTotale = 50000,
            Classements = new List<Classement>() // Empty classements
        };
        _context.HistoriquesClassement.Add(historique);
        await _context.SaveChangesAsync();

        // Act
        await _service.UpdateClassementEditableAsync(
            historique.Id,
            new DateOnly(2025, 2, 20),
            ligue: 5,
            score: 5000,
            nutaku: 100,
            top150: 50,
            france: 25);

        // Assert
        var updated = await _context.HistoriquesClassement
            .Include(h => h.Classements)
            .FirstAsync(h => h.Id == historique.Id);
        
        Assert.Equal(3, updated.Classements.Count);
    }

    #endregion

    #region DeserializerEscouade Tests

    [Fact]
    public void DeserializerEscouade_ShouldReturnNull_WhenInvalidJson()
    {
        // Act
        var result = HistoriqueClassementService.DeserializerEscouade("invalid json");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void DeserializerEscouade_ShouldDeserializeValidJson()
    {
        // Arrange
        var json = JsonSerializer.Serialize(new DonneesEscouadeSerialisees
        {
            Ligue = 5,
            Score = 10000,
            Nutaku = 100
        });

        // Act
        var result = HistoriqueClassementService.DeserializerEscouade(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result!.Ligue);
        Assert.Equal(10000, result.Score);
        Assert.Equal(100, result.Nutaku);
    }

    [Fact]
    public void DeserializerEscouade_ShouldDeserializeWithMercenaires()
    {
        // Arrange
        var donnees = new DonneesEscouadeSerialisees
        {
            Mercenaires = new List<PersonnelHistorique>
            {
                new() { Nom = "ALICE", Niveau = 100, Puissance = 5000 }
            },
            Ligue = 3
        };
        var json = JsonSerializer.Serialize(donnees);

        // Act
        var result = HistoriqueClassementService.DeserializerEscouade(json);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result!.Mercenaires);
        Assert.Equal("ALICE", result.Mercenaires[0].Nom);
    }

    #endregion

    #region Helper Methods

    private async Task CreateTestHistoriques()
    {
        _context.HistoriquesClassement.AddRange(
            CreateHistoriqueClassement(new DateOnly(2025, 1, 5)),
            CreateHistoriqueClassement(new DateOnly(2025, 1, 15)),
            CreateHistoriqueClassement(new DateOnly(2025, 1, 25)));
        await _context.SaveChangesAsync();
    }

    private static HistoriqueClassement CreateHistoriqueClassement(DateOnly date)
    {
        return new HistoriqueClassement
        {
            DateEnregistrement = date,
            Ligue = 10,
            Score = 1000,
            PuissanceTotale = 50000,
            Classements = new List<Classement>
            {
                new() { Nom = "Nutaku", Type = TypeClassement.Nutaku, Valeur = 100 },
                new() { Nom = "Top150", Type = TypeClassement.Top150, Valeur = 50 },
                new() { Nom = "France", Type = TypeClassement.France, Valeur = 25 }
            }
        };
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
