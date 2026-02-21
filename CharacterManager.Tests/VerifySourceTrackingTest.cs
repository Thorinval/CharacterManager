using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Models.Enums;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CharacterManager.Tests;

/// <summary>
/// Test pour vérifier que les sources sont bien enregistrées dans l'historique
/// </summary>
public class VerifySourceTrackingTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly HistoriqueModificationService _historiqueService;

    public VerifySourceTrackingTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=D:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options;

        _context = new ApplicationDbContext(options);
        _historiqueService = new HistoriqueModificationService(_context);
    }

    [Fact]
    public async Task TestSourceTracking_AllSources()
    {
        var timestamp = DateTime.UtcNow;

        // Cleanup any existing test data first using a fresh context to avoid tracking issues
        using (var cleanupContextBefore = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite("Data Source=D:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
                .Options))
        {
            var existingRecords = await cleanupContextBefore.HistoriquesModifications
                .Where(h => h.EntiteId >= 999900 && h.EntiteId <= 999999)
                .ToListAsync();
            if (existingRecords.Count > 0)
            {
                cleanupContextBefore.HistoriquesModifications.RemoveRange(existingRecords);
                await cleanupContextBefore.SaveChangesAsync();
            }
        }

        // Test 1: Création depuis Inventaire
        await _historiqueService.EnregistrerCreationAsync(
            TypeEntite.Personnage,
            999901,
            "Test Inventaire",
            new { Nom = "Test", Puissance = 100 },
            "Test création Inventaire",
            timestamp,
            estImportation: false,
            source: SourceModification.Inventaire);

        // Test 2: Modification depuis ImportPml
        await _historiqueService.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            999902,
            "Test PML",
            "Puissance",
            100,
            150,
            "Test modification PML",
            timestamp,
            estImportation: true,
            source: SourceModification.ImportPml);

        // Test 3: Suppression depuis ImportClassement
        await _historiqueService.EnregistrerSuppressionAsync(
            TypeEntite.Personnage,
            999903,
            "Test Classement",
            new { Nom = "Test", Puissance = 200 },
            "Test suppression Classement",
            timestamp,
            estImportation: true,
            source: SourceModification.ImportClassement);

        // Test 4: Ancien historique (NonSpecifiee)
        await _historiqueService.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            999904,
            "Test NonSpecifiee",
            "Niveau",
            1,
            2,
            "Test ancienne donnée",
            timestamp);

        // Vérification
        var historiques = await _context.HistoriquesModifications
            .Where(h => h.EntiteId >= 999901 && h.EntiteId <= 999904)
            .OrderBy(h => h.EntiteId)
            .ToListAsync();

        Assert.Equal(4, historiques.Count);
        
        // Test 1: Source = Inventaire
        Assert.Equal(SourceModification.Inventaire, historiques[0].Source);
        Assert.Equal("Test Inventaire", historiques[0].NomEntite);
        Assert.Equal(TypeModification.Creation, historiques[0].TypeModification);

        // Test 2: Source = ImportPml
        Assert.Equal(SourceModification.ImportPml, historiques[1].Source);
        Assert.Equal("Test PML", historiques[1].NomEntite);
        Assert.Equal(TypeModification.Modification, historiques[1].TypeModification);

        // Test 3: Source = ImportClassement
        Assert.Equal(SourceModification.ImportClassement, historiques[2].Source);
        Assert.Equal("Test Classement", historiques[2].NomEntite);
        Assert.Equal(TypeModification.Suppression, historiques[2].TypeModification);

        // Test 4: Source = NonSpecifiee (par défaut)
        Assert.Equal(SourceModification.NonSpecifiee, historiques[3].Source);
        Assert.Equal("Test NonSpecifiee", historiques[3].NomEntite);
        Assert.Equal(TypeModification.Modification, historiques[3].TypeModification);

        // Cleanup - use fresh context to avoid concurrency issues
        using (var cleanupContext = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite("Data Source=D:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
                .Options))
        {
            var recordsToDelete = await cleanupContext.HistoriquesModifications
                .Where(h => h.EntiteId >= 999900 && h.EntiteId <= 999999)
                .AsNoTracking()
                .ToListAsync();
            
            if (recordsToDelete.Count > 0)
            {
                foreach (var record in recordsToDelete)
                {
                    cleanupContext.HistoriquesModifications.Remove(record);
                }
                try
                {
                    await cleanupContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    // If concurrency exception, reload and try again
                    cleanupContext.ChangeTracker.Clear();
                    var reloadedRecords = await cleanupContext.HistoriquesModifications
                        .Where(h => h.EntiteId >= 999900 && h.EntiteId <= 999999)
                        .ToListAsync();
                    cleanupContext.HistoriquesModifications.RemoveRange(reloadedRecords);
                    await cleanupContext.SaveChangesAsync();
                }
            }
        }

        Console.WriteLine("✅ Tous les tests de source tracking réussis!");
        Console.WriteLine($"   - Inventaire: {historiques[0].Source} ✓");
        Console.WriteLine($"   - ImportPml: {historiques[1].Source} ✓");
        Console.WriteLine($"   - ImportClassement: {historiques[2].Source} ✓");
        Console.WriteLine($"   - NonSpecifiee: {historiques[3].Source} ✓");
    }

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

    ~VerifySourceTrackingTest()
    {
        Dispose(false);
    }
}
