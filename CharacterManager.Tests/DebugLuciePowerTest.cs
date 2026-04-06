using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace CharacterManager.Tests;

public class DebugLuciePowerTest
{
    private readonly ITestOutputHelper _output;

    public DebugLuciePowerTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DebugLuciePowerCalculation()
    {
        // Connexion à la vraie base de données
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=d:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options;

        using var context = new ApplicationDbContext(options);
        
        // Dernières modifications Lucie Max
        var lucieMaxHistory = context.HistoriquesModifications
            .Where(h => h.EntiteId == -2 && h.TypeEntite == TypeEntite.Piece && h.ChampModifie == "PuissanceLucieMax")
            .OrderByDescending(h => h.DateModification)
            .Take(10)
            .ToList();

        _output.WriteLine("=== HISTORIQUE LUCIE MAX (10 dernières) ===");
        foreach (var h in lucieMaxHistory)
        {
            _output.WriteLine($"{h.DateModification:dd/MM/yyyy HH:mm:ss} - {h.AncienneValeur} → {h.NouvelleValeur}");
        }

        _output.WriteLine($"\nAujourd'hui: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        _output.WriteLine($"Date.Date: {DateTime.Now.Date:dd/MM/yyyy HH:mm:ss}");

        // Simuler l'appel avec 26/01/2026 00:00:00
        var testDate = new DateTime(2026, 1, 26, 0, 0, 0);
        _output.WriteLine($"\nTest avec date: {testDate:dd/MM/yyyy HH:mm:ss}");
        _output.WriteLine($"testDate.Date: {testDate.Date:dd/MM/yyyy HH:mm:ss}");
        _output.WriteLine($"testDate.Date == DateTime.Now.Date: {testDate.Date == DateTime.Now.Date}");

        var historyAtTestDate = context.HistoriquesModifications
            .Where(h => h.EntiteId == -2 
                     && h.TypeEntite == TypeEntite.Piece
                     && h.ChampModifie == "PuissanceLucieMax"
                     && h.DateModification <= testDate)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefault();

        if (historyAtTestDate != null)
        {
            _output.WriteLine($"\nDernière modif <= {testDate:dd/MM/yyyy HH:mm:ss}:");
            _output.WriteLine($"  Date: {historyAtTestDate.DateModification:dd/MM/yyyy HH:mm:ss}");
            _output.WriteLine($"  Valeur: {historyAtTestDate.NouvelleValeur}");
        }
        else
        {
            _output.WriteLine($"\nAucune modification trouvée <= {testDate:dd/MM/yyyy HH:mm:ss}");
        }
    }
}
