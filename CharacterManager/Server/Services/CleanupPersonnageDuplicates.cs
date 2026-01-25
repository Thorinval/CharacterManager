using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;

namespace CharacterManager.Scripts;

/// <summary>
/// Script de nettoyage des doublons dans la table Personnages
/// </summary>
public class CleanupPersonnageDuplicates
{
    private readonly ApplicationDbContext _context;

    public CleanupPersonnageDuplicates(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CleanupResult> ExecuteAsync(bool dryRun = true)
    {
        var result = new CleanupResult();

        Console.WriteLine("=== Nettoyage des doublons de personnages ===");
        Console.WriteLine($"Mode: {(dryRun ? "DRY RUN (aucune modification)" : "EXECUTION")}");
        Console.WriteLine();

        // 1. Identifier les doublons
        var duplicates = await IdentifyDuplicatesAsync();
        result.DuplicatesFound = duplicates.Count;

        if (duplicates.Count == 0)
        {
            Console.WriteLine("✓ Aucun doublon trouvé !");
            return result;
        }

        Console.WriteLine($"⚠ {duplicates.Count} groupe(s) de doublons trouvé(s):");
        foreach (var dup in duplicates)
        {
            Console.WriteLine($"  - '{dup.NomUpper}': {dup.DuplicateIds.Count} entrées (IDs: {string.Join(", ", dup.DuplicateIds)})");
            Console.WriteLine($"    Garder ID {dup.IdToKeep}, supprimer: {string.Join(", ", dup.IdsToDelete)}");
        }
        Console.WriteLine();

        if (dryRun)
        {
            Console.WriteLine("Mode DRY RUN - Aucune modification effectuée.");
            Console.WriteLine("Exécutez avec dryRun=false pour appliquer les changements.");
            return result;
        }

        // 2. Mettre à jour les références dans HistoriquesModifications
        result.HistoriquesUpdated = await UpdateHistoriquesModificationsAsync(duplicates);
        Console.WriteLine($"✓ {result.HistoriquesUpdated} référence(s) mise(s) à jour dans HistoriquesModifications");

        // 3. Mettre à jour les références dans Templates
        result.TemplatesUpdated = await UpdateTemplatesAsync(duplicates);
        Console.WriteLine($"✓ {result.TemplatesUpdated} template(s) mis à jour");

        // 4. Supprimer les doublons
        result.PersonnagesDeleted = await DeleteDuplicatesAsync(duplicates);
        Console.WriteLine($"✓ {result.PersonnagesDeleted} personnage(s) supprimé(s)");

        // 5. Vérifier qu'il n'y a plus de doublons
        var remainingDuplicates = await IdentifyDuplicatesAsync();
        if (remainingDuplicates.Count == 0)
        {
            Console.WriteLine("✓ Nettoyage terminé avec succès - Aucun doublon restant");
        }
        else
        {
            Console.WriteLine($"⚠ ATTENTION: {remainingDuplicates.Count} doublon(s) restant(s) après nettoyage!");
        }

        return result;
    }

    private async Task<List<DuplicateGroup>> IdentifyDuplicatesAsync()
    {
        var personnages = await _context.Personnages.ToListAsync();
        
        var grouped = personnages
            .GroupBy(p => p.Nom.ToUpperInvariant())
            .Where(g => g.Count() > 1)
            .Select(g => new DuplicateGroup
            {
                NomUpper = g.Key,
                DuplicateIds = g.Select(p => p.Id).ToList(),
                IdToKeep = g.OrderBy(p => p.Id).First().Id, // Garder le plus ancien (plus petit ID)
                IdsToDelete = g.OrderBy(p => p.Id).Skip(1).Select(p => p.Id).ToList()
            })
            .ToList();

        return grouped;
    }

    private async Task<int> UpdateHistoriquesModificationsAsync(List<DuplicateGroup> duplicates)
    {
        var idMap = BuildDuplicateMap(duplicates);
        if (idMap.Count == 0)
        {
            return 0;
        }

        var idsToReplace = idMap.Keys.ToList();

        var historiques = await _context.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage && idsToReplace.Contains(h.EntiteId))
            .ToListAsync();

        foreach (var historique in historiques)
        {
            historique.EntiteId = idMap[historique.EntiteId];
        }

        if (historiques.Count > 0)
        {
            await _context.SaveChangesAsync();
        }

        return historiques.Count;
    }

    private async Task<int> UpdateTemplatesAsync(List<DuplicateGroup> duplicates)
    {
        var idMap = BuildDuplicateMap(duplicates);
        if (idMap.Count == 0)
        {
            return 0;
        }

        int updateCount = 0;
        var templates = await _context.Templates.ToListAsync();

        foreach (var template in templates)
        {
            var personnageIds = template.GetPersonnageIds();
            var updatedIds = personnageIds
                .Select(id => idMap.TryGetValue(id, out var keepId) ? keepId : id)
                .Distinct()
                .ToList();

            if (!updatedIds.SequenceEqual(personnageIds))
            {
                template.SetPersonnageIds(updatedIds);
                updateCount++;
            }
        }

        if (updateCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        return updateCount;
    }

    private async Task<int> DeleteDuplicatesAsync(List<DuplicateGroup> duplicates)
    {
        int deleteCount = 0;

        foreach (var dup in duplicates)
        {
            foreach (var idToDelete in dup.IdsToDelete)
            {
                var personnage = await _context.Personnages.FindAsync(idToDelete);
                if (personnage != null)
                {
                    _context.Personnages.Remove(personnage);
                    deleteCount++;
                }
            }
        }

        if (deleteCount > 0)
        {
            await _context.SaveChangesAsync();
        }

        return deleteCount;
    }

    private static Dictionary<int, int> BuildDuplicateMap(IEnumerable<DuplicateGroup> duplicates)
    {
        return duplicates
            .SelectMany(dup => dup.IdsToDelete.Select(id => new { id, dup.IdToKeep }))
            .ToDictionary(x => x.id, x => x.IdToKeep);
    }

    public class DuplicateGroup
    {
        public string NomUpper { get; set; } = string.Empty;
        public List<int> DuplicateIds { get; set; } = new();
        public int IdToKeep { get; set; }
        public List<int> IdsToDelete { get; set; } = new();
    }

    public class CleanupResult
    {
        public int DuplicatesFound { get; set; }
        public int HistoriquesUpdated { get; set; }
        public int TemplatesUpdated { get; set; }
        public int PersonnagesDeleted { get; set; }
    }
}
