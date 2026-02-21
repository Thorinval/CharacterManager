using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharacterManager.Tests;

/// <summary>
/// Test pour identifier les doublons de personnages
/// </summary>
public class DetectDuplicatePersonnagesTest : IDisposable
{
    private readonly ApplicationDbContext _context;

    public DetectDuplicatePersonnagesTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=D:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task DetectDuplicatePersonnages()
    {
        Console.WriteLine("\n🔍 Recherche de doublons de personnages...\n");

        // Rechercher tous les personnages groupés par nom
        var duplicates = await _context.Personnages
            .GroupBy(p => p.Nom.ToUpper())
            .Select(g => new
            {
                Nom = g.Key,
                Count = g.Count(),
                Personnages = g.Select(p => new { p.Id, p.Nom, p.Type, p.Niveau, p.Puissance }).ToList()
            })
            .Where(g => g.Count > 1)
            .ToListAsync();

        if (duplicates.Any())
        {
            Console.WriteLine($"⚠️  {duplicates.Count} nom(s) en doublon trouvé(s):\n");
            
            foreach (var dup in duplicates.OrderBy(d => d.Nom))
            {
                Console.WriteLine($"  📛 {dup.Nom} ({dup.Count} occurrences):");
                foreach (var perso in dup.Personnages.OrderBy(p => p.Id))
                {
                    Console.WriteLine($"      - ID={perso.Id}, Type={perso.Type}, Niveau={perso.Niveau}, Puissance={perso.Puissance}");
                }
                Console.WriteLine();
            }

            // Vérifier si LETA est en doublon
            var leta = duplicates.FirstOrDefault(d => d.Nom == "LETA");
            if (leta != null)
            {
                Console.WriteLine("🎯 Détails pour LETA:");
                Console.WriteLine($"   Nombre d'occurrences: {leta.Count}");
                foreach (var perso in leta.Personnages)
                {
                    Console.WriteLine($"   ID={perso.Id}: {perso.Nom}");
                    
                    // Compter les modifications pour chaque ID
                    var modifCount = await _context.HistoriquesModifications
                        .Where(h => h.TypeEntite == TypeEntite.Personnage 
                                 && h.EntiteId == perso.Id)
                        .CountAsync();
                    
                    Console.WriteLine($"      → {modifCount} modifications dans l'historique");
                }
            }
        }
        else
        {
            Console.WriteLine("✅ Aucun doublon trouvé!\n");
        }

        Assert.True(true, "Test de diagnostic");
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
