using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace CharacterManager.Tests;

public class CleanLucieHistoryTest
{
    private readonly ITestOutputHelper _output;

    public CleanLucieHistoryTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CleanLucieHistoryEntries()
    {
        // Connexion à la vraie base de données
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=d:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options;

        using var context = new ApplicationDbContext(options);
        
        // 1. Compter les enregistrements à supprimer
        var lucieSelectCount = context.HistoriquesModifications
            .Count(h => h.TypeEntite == TypeEntite.Piece && h.EntiteId == -1);
        
        var lucieMaxCount = context.HistoriquesModifications
            .Count(h => h.TypeEntite == TypeEntite.Piece && h.EntiteId == -2);

        _output.WriteLine("=== AVANT SUPPRESSION ===");
        _output.WriteLine($"Enregistrements Lucie Select (EntiteId=-1): {lucieSelectCount}");
        _output.WriteLine($"Enregistrements Lucie Max (EntiteId=-2): {lucieMaxCount}");
        _output.WriteLine($"Total à supprimer: {lucieSelectCount + lucieMaxCount}");
        _output.WriteLine("");

        // 2. Afficher quelques exemples
        var examples = context.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Piece && (h.EntiteId == -1 || h.EntiteId == -2))
            .OrderBy(h => h.DateModification)
            .Take(10)
            .ToList();

        _output.WriteLine("Exemples (10 premiers):");
        foreach (var ex in examples)
        {
            _output.WriteLine($"  {ex.DateModification:dd/MM/yyyy HH:mm} - EntiteId={ex.EntiteId} - {ex.ChampModifie}: {ex.AncienneValeur}→{ex.NouvelleValeur}");
        }
        _output.WriteLine("");

        // 3. Supprimer
        var toDelete = context.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Piece && (h.EntiteId == -1 || h.EntiteId == -2))
            .ToList();

        context.HistoriquesModifications.RemoveRange(toDelete);
        var deletedCount = context.SaveChanges();

        _output.WriteLine("=== APRÈS SUPPRESSION ===");
        _output.WriteLine($"Enregistrements supprimés: {deletedCount}");

        // 4. Vérifier
        var remaining = context.HistoriquesModifications
            .Count(h => h.TypeEntite == TypeEntite.Piece && (h.EntiteId == -1 || h.EntiteId == -2));

        _output.WriteLine($"Enregistrements restants (EntiteId=-1 ou -2): {remaining}");
        
        Assert.Equal(0, remaining);
    }
}
