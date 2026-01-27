using CharacterManager.Server.Data;
using CharacterManager.Server.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharacterManager.Tests;

/// <summary>
/// Test pour visualiser la répartition des sources dans l'historique
/// </summary>
public class ShowSourceDistributionTest : IDisposable
{
    private readonly ApplicationDbContext _context;

    public ShowSourceDistributionTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=D:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options;

        _context = new ApplicationDbContext(options);
    }

    [Fact]
    public async Task ShowSourceDistribution()
    {
        var distribution = await _context.HistoriquesModifications
            .GroupBy(h => h.Source)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .OrderBy(x => x.Source)
            .ToListAsync();

        Console.WriteLine("\n📊 Répartition des sources dans l'historique:");
        Console.WriteLine("═══════════════════════════════════════════════");
        
        foreach (var item in distribution)
        {
            var sourceName = item.Source switch
            {
                SourceModification.NonSpecifiee => "NonSpecifiee (anciennes données)",
                SourceModification.Inventaire => "Inventaire (modifications manuelles)",
                SourceModification.ImportPml => "ImportPml (imports fichiers .pml)",
                SourceModification.ImportClassement => "ImportClassement (imports rankings)",
                _ => item.Source.ToString()
            };
            Console.WriteLine($"  {sourceName,-50} {item.Count,6} entrées");
        }
        
        Console.WriteLine("═══════════════════════════════════════════════");
        Console.WriteLine($"  TOTAL: {distribution.Sum(x => x.Count)} entrées\n");

        Assert.NotEmpty(distribution);
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
