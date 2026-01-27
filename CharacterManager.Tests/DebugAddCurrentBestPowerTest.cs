using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Xunit.Abstractions;

namespace CharacterManager.Tests;

public class DebugAddCurrentBestPowerTest
{
    private readonly ITestOutputHelper _output;

    public DebugAddCurrentBestPowerTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Debug_AddCurrentBestPower_Logic()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=d:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db")
            .Options;

        using var context = new ApplicationDbContext(options);
        var historiqueService = new HistoriqueModificationService(context);
        var personnageService = new PersonnageService(context, historiqueService, NullLogger<PersonnageService>.Instance);

        // Valeurs live
        var liveMaxEscouade = personnageService.GetPuissanceMaxEscouade();
        var liveLucieMax = personnageService.GetPuissanceMaxLucieEscouade();
        var livePersonnagesPower = liveMaxEscouade - liveLucieMax;

        _output.WriteLine($"=== VALEURS LIVE ===");
        _output.WriteLine($"GetPuissanceMaxEscouade(): {liveMaxEscouade}");
        _output.WriteLine($"GetPuissanceMaxLucieEscouade(): {liveLucieMax}");
        _output.WriteLine($"Personnages (Mercs + Androids + Cmd): {livePersonnagesPower}");
        _output.WriteLine("");

        // Simuler l'appel de CalculateBestTeamPowerAtDateForDateTime(DateTime.Now)
        _output.WriteLine($"=== SIMULATION CalculateBestTeamPowerAtDateForDateTime(DateTime.Now) ===");
        _output.WriteLine($"DateTime.Now: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        _output.WriteLine($"DateTime.Now.Date: {DateTime.Now.Date:dd/MM/yyyy HH:mm:ss}");
        
        var now = DateTime.Now;
        _output.WriteLine("");
        _output.WriteLine($"Condition: now.Date == DateTime.Now.Date ?");
        _output.WriteLine($"  {now.Date:dd/MM/yyyy HH:mm:ss} == {DateTime.Now.Date:dd/MM/yyyy HH:mm:ss}");
        _output.WriteLine($"  Result: {now.Date == DateTime.Now.Date}");
        _output.WriteLine("");

        // Vérifier l'historique pour aujourd'hui
        var today = DateTime.Now.Date;
        var lastLucieMaxHistory = context.HistoriquesModifications
            .Where(h => h.EntiteId == -2 
                     && h.TypeEntite == TypeEntite.Piece
                     && h.ChampModifie == "PuissanceLucieMax"
                     && h.DateModification <= today)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefault();

        _output.WriteLine($"Dernière modif Lucie Max <= {today:dd/MM/yyyy HH:mm:ss}:");
        if (lastLucieMaxHistory != null)
        {
            _output.WriteLine($"  Date: {lastLucieMaxHistory.DateModification:dd/MM/yyyy HH:mm:ss}");
            _output.WriteLine($"  Valeur: {lastLucieMaxHistory.NouvelleValeur}");
            _output.WriteLine($"  (Live Lucie Max: {liveLucieMax})");
        }
        else
        {
            _output.WriteLine("  Aucune");
        }
    }
}
