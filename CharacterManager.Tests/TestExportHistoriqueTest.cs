using CharacterManager.Server.Data;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CharacterManager.Tests;

/// <summary>
/// Test pour vérifier que l'export d'historique fonctionne correctement
/// </summary>
public class TestExportHistoriqueTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly HistoriqueModificationService _service;

    public TestExportHistoriqueTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=D:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new HistoriqueModificationService(_context);
    }

    [Fact]
    public async Task TestExportHistorique_ShouldGenerateValidJson()
    {
        // Test 1: Export du dernier mois
        Console.WriteLine("\n📥 Test 1: Export du dernier mois");
        var dateDebut = DateTime.Now.AddMonths(-1);
        var dateFin = DateTime.Now;
        
        var json1 = await _service.ExporterAsync(dateDebut, dateFin);
        
        Assert.NotNull(json1);
        Assert.NotEmpty(json1);
        Assert.Contains("\"TypeEntite\"", json1);
        Assert.Contains("\"Source\"", json1);
        
        Console.WriteLine($"   ✅ JSON généré: {json1.Length:N0} caractères ({json1.Length / 1024.0:F2} KB)");

        // Test 2: Export complet
        Console.WriteLine("\n📥 Test 2: Export complet");
        var json2 = await _service.ExporterToutAsync();
        
        Assert.NotNull(json2);
        Assert.NotEmpty(json2);
        
        Console.WriteLine($"   ✅ JSON généré: {json2.Length:N0} caractères ({json2.Length / 1024.0:F2} KB)");

        // Test 3: Vérifier que le JSON est désérialisable
        Console.WriteLine("\n🔍 Test 3: Vérification de la désérialisation");
        try
        {
            var items = System.Text.Json.JsonSerializer.Deserialize<List<object>>(json1);
            Assert.NotNull(items);
            Console.WriteLine($"   ✅ Désérialisation réussie: {items.Count} éléments");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Erreur de désérialisation: {ex.Message}");
        }

        Console.WriteLine("\n✅ Tous les tests d'export réussis!");
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
