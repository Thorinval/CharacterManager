using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Models.Enums;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Tests;

/// <summary>
/// Test pour vérifier qu'on ne crée pas de doublons lors de l'import
/// </summary>
public class TestNoDuplicateHistoriqueOnImportTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly HistoriqueModificationService _historiqueService;

    public TestNoDuplicateHistoriqueOnImportTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=D:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options;

        _context = new ApplicationDbContext(options);
        _historiqueService = new HistoriqueModificationService(_context);
    }

    [Fact]
    public async Task EnregistrerModificationAsync_SameDay_ShouldUpdate_NotDuplicate()
    {
        var testDate = DateTime.Now.Date.AddHours(10);
        var testPersonnageId = 999901;
        var testNom = "TEST_DUPLICATE";

        // Nettoyer les données de test précédentes
        var existingTests = await _context.HistoriquesModifications
            .Where(h => h.EntiteId == testPersonnageId)
            .ToListAsync();
        _context.HistoriquesModifications.RemoveRange(existingTests);
        await _context.SaveChangesAsync();

        Console.WriteLine($"\n🧪 Test de non-duplication d'historique");
        Console.WriteLine($"   Date de test: {testDate:dd/MM/yyyy HH:mm:ss}");
        Console.WriteLine($"   Personnage ID: {testPersonnageId}, Nom: {testNom}\n");

        // 1. Créer une première modification manuelle (EstImportation=false)
        Console.WriteLine("1️⃣  Création modification manuelle...");
        await _historiqueService.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            testPersonnageId,
            testNom,
            "Puissance",
            1000,
            1200,
            "Modification manuelle",
            testDate,
            estImportation: false,
            source: SourceModification.Inventaire);

        var count1 = await _context.HistoriquesModifications
            .Where(h => h.EntiteId == testPersonnageId 
                     && h.ChampModifie == "Puissance"
                     && h.DateModification.Date == testDate.Date)
            .CountAsync();

        Console.WriteLine($"   ✅ {count1} entrée(s) créée(s)");
        Assert.Equal(1, count1);

        // 2. Créer une modification d'import le même jour (EstImportation=true)
        Console.WriteLine("\n2️⃣  Tentative de création via import (même jour)...");
        await _historiqueService.EnregistrerModificationAsync(
            TypeEntite.Personnage,
            testPersonnageId,
            testNom,
            "Puissance",
            1200,
            1300,
            "Import classement",
            testDate.AddHours(3),
            estImportation: true,
            source: SourceModification.ImportClassement);

        var count2 = await _context.HistoriquesModifications
            .Where(h => h.EntiteId == testPersonnageId 
                     && h.ChampModifie == "Puissance"
                     && h.DateModification.Date == testDate.Date)
            .CountAsync();

        Console.WriteLine($"   ✅ {count2} entrée(s) au total");
        
        // VÉRIFICATION: Il ne doit y avoir qu'UNE SEULE entrée (mise à jour, pas doublon)
        Assert.Equal(1, count2);

        // 3. Vérifier que la valeur finale est correcte
        var finalEntry = await _context.HistoriquesModifications
            .Where(h => h.EntiteId == testPersonnageId 
                     && h.ChampModifie == "Puissance"
                     && h.DateModification.Date == testDate.Date)
            .FirstAsync();

        Console.WriteLine($"\n3️⃣  Vérification de l'entrée finale:");
        Console.WriteLine($"   - NouvelleValeur: {finalEntry.NouvelleValeur}");
        Console.WriteLine($"   - EstImportation: {finalEntry.EstImportation}");
        Console.WriteLine($"   - Source: {finalEntry.Source}");
        Console.WriteLine($"   - DateModification: {finalEntry.DateModification:dd/MM/yyyy HH:mm:ss}");

        Assert.Equal("1300", finalEntry.NouvelleValeur);
        Assert.True(finalEntry.EstImportation);
        Assert.Equal(SourceModification.ImportClassement, finalEntry.Source);

        // Nettoyage - utiliser un nouveau contexte pour éviter les problèmes de concurrence
        using (var cleanupContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=D:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options))
        {
            try
            {
                var entryToDelete = await cleanupContext.HistoriquesModifications.FirstAsync(h => h.Id == finalEntry.Id);
                cleanupContext.HistoriquesModifications.Remove(entryToDelete);
                await cleanupContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // If concurrency exception, reload and try again
                cleanupContext.ChangeTracker.Clear();
                var reloadedEntry = await cleanupContext.HistoriquesModifications.FirstOrDefaultAsync(h => h.Id == finalEntry.Id);
                if (reloadedEntry != null)
                {
                    cleanupContext.HistoriquesModifications.Remove(reloadedEntry);
                    await cleanupContext.SaveChangesAsync();
                }
            }
        }

        Console.WriteLine($"\n✅ TEST RÉUSSI: Pas de doublon créé lors de l'import!");
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
