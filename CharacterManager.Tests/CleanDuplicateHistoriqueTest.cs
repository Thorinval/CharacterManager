using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Xunit;

namespace CharacterManager.Tests;

/// <summary>
/// Utilitaire pour nettoyer les doublons dans l'historique des modifications
/// </summary>
public class CleanDuplicateHistoriqueTest : IDisposable
{
    private readonly ApplicationDbContext _context;

    public CleanDuplicateHistoriqueTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=D:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CleanDuplicateHistoriqueSameDay()
    {
        Console.WriteLine("\n🧹 Nettoyage des doublons dans l'historique...\n");

        // Trouver tous les groupes de modifications pour le même jour/entité/champ
        var allModifications = await _context.HistoriquesModifications
            .Where(h => h.TypeModification == TypeModification.Modification)
            .OrderBy(h => h.DateModification)
            .ToListAsync();

        // Grouper par jour, entité, champ et nom d'entité (pour gérer les changements d'ID)
        var duplicateGroups = allModifications
            .GroupBy(h => new
            {
                Date = h.DateModification.Date,
                h.TypeEntite,
                h.NomEntite,
                h.ChampModifie
            })
            .Where(g => g.Count() > 1)
            .ToList();

        Console.WriteLine($"📊 {duplicateGroups.Count} groupe(s) avec doublons trouvé(s)\n");

        int totalRemoved = 0;
        foreach (var group in duplicateGroups)
        {
            var items = group.OrderBy(h => h.DateInsertion).ToList();
            
            Console.WriteLine($"🔍 {group.Key.NomEntite} - {group.Key.ChampModifie} - {group.Key.Date:dd/MM/yyyy}");
            Console.WriteLine($"   {items.Count} entrées:");

            // Analyser les valeurs pour déterminer laquelle garder
            var valueCounts = items
                .GroupBy(h => h.NouvelleValeur)
                .Select(g => new { Value = g.Key, Count = g.Count(), Items = g.ToList() })
                .OrderByDescending(x => x.Count)
                .ToList();

            // Stratégie: Garder la dernière entrée insérée avec la valeur la plus courante
            var valueToKeep = valueCounts.First().Value;
            var itemToKeep = items
                .Where(h => h.NouvelleValeur == valueToKeep)
                .OrderByDescending(h => h.DateInsertion)
                .First();

            foreach (var item in items)
            {
                var val = item.NouvelleValeur != null 
                    ? JsonSerializer.Deserialize<object>(item.NouvelleValeur)?.ToString()
                    : "null";
                var keep = item.Id == itemToKeep.Id ? "✅ GARDER" : "❌ SUPPRIMER";
                
                Console.WriteLine($"      - ID={item.Id}, EntiteId={item.EntiteId}, Val={val}, " +
                    $"Insertion={item.DateInsertion:HH:mm:ss}, EstImport={item.EstImportation}, " +
                    $"Source={item.Source} {keep}");
            }

            // Supprimer les doublons (tous sauf celui à garder)
            var toRemove = items.Where(h => h.Id != itemToKeep.Id).ToList();
            _context.HistoriquesModifications.RemoveRange(toRemove);
            totalRemoved += toRemove.Count;
            
            Console.WriteLine();
        }

        if (totalRemoved > 0)
        {
            Console.WriteLine($"💾 Enregistrement des suppressions ({totalRemoved} entrées)...");
            await _context.SaveChangesAsync();
            Console.WriteLine($"✅ {totalRemoved} doublon(s) supprimé(s) avec succès!\n");
        }
        else
        {
            Console.WriteLine("✨ Aucun doublon à nettoyer!\n");
        }

        // Vérification finale
        var remainingDuplicates = allModifications
            .GroupBy(h => new
            {
                Date = h.DateModification.Date,
                h.TypeEntite,
                h.NomEntite,
                h.ChampModifie
            })
            .Count(g => g.Count() > 1);

        Console.WriteLine($"📈 Statut final: {remainingDuplicates} groupe(s) avec doublons restant(s)");
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
