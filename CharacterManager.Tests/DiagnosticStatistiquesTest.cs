using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;
using System.Text;

namespace CharacterManager.Tests;

public class DiagnosticStatistiquesTest
{
    public DiagnosticStatistiquesTest(ITestOutputHelper output)
    {
    }

    [Fact]
    public void GetSelectedTeamPowerEvolutionData_ReturnsData()
    {
        using var context = CreateDbContext();
        var service = CreateStatistiquesService(context);

        var result = service.GetSelectedTeamPowerEvolutionData();

        Assert.NotNull(result);
    }

    [Fact]
    public void GetBestTeamPowerEvolutionData_ReturnsData()
    {
        using var context = CreateDbContext();
        var service = CreateStatistiquesService(context);

        var result = service.GetBestTeamPowerEvolutionData();

        Assert.NotNull(result);
    }

    [Fact]
    public void GenerateDetailedPowerEvolutionReport_CreatesReportFile()
    {
        using var context = CreateDbContext();
        var historiqueService = new HistoriqueModificationService(context);
        var loggerMock = new Mock<ILogger<PersonnageService>>();
        var personnageService = new PersonnageService(context, historiqueService, loggerMock.Object);
        var service = new StatistiquesService(context, personnageService);

        var report = GenerateReport(context, service, personnageService);
        var reportPath = Path.Combine("d:\\Devs\\CharacterManager", $"diagnostic_power_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(reportPath, report);

        Assert.True(File.Exists(reportPath));
        File.Delete(reportPath);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite("Data Source=d:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db");
        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static StatistiquesService CreateStatistiquesService(ApplicationDbContext context)
    {
        var historiqueService = new HistoriqueModificationService(context);
        var loggerMock = new Mock<ILogger<PersonnageService>>();
        var personnageService = new PersonnageService(context, historiqueService, loggerMock.Object);
        return new StatistiquesService(context, personnageService);
    }

    private static string GenerateReport(ApplicationDbContext context, StatistiquesService service, PersonnageService personnageService)
    {
        var report = new StringBuilder();
        report.AppendLine("=== RAPPORT DÉTAILLÉ ÉVOLUTION PUISSANCE ===");
        report.AppendLine($"Généré le {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        report.AppendLine();

        AppendClassementInfo(context, report);
        AppendModificationHistoriqueInfo(context, report);
        AppendTimelineComparison(context, service, report);
        AppendCurrentValues(personnageService, report);
        AppendTopCharacters(personnageService, report);

        return report.ToString();
    }

    private static void AppendClassementInfo(ApplicationDbContext context, StringBuilder report)
    {
        var classements = context.HistoriquesClassement
            .OrderBy(h => h.DateEnregistrement)
            .ToList();
        
        report.AppendLine($"CLASSEMENTS ENREGISTRÉS: {classements.Count}");
        report.AppendLine($"Premier: {classements.FirstOrDefault()?.DateEnregistrement:dd/MM/yyyy} - Puissance: {classements.FirstOrDefault()?.PuissanceTotale}");
        report.AppendLine($"Dernier: {classements.LastOrDefault()?.DateEnregistrement:dd/MM/yyyy} - Puissance: {classements.LastOrDefault()?.PuissanceTotale}");
        report.AppendLine();
    }

    private static void AppendModificationHistoriqueInfo(ApplicationDbContext context, StringBuilder report)
    {
        var firstPersonMod = context.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage)
            .OrderBy(h => h.DateModification)
            .FirstOrDefault();
        
        var firstPieceMod = context.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Piece && h.EntiteId == -2)
            .OrderBy(h => h.DateModification)
            .FirstOrDefault();

        report.AppendLine($"MODIFICATIONS HISTORIQUES:");
        report.AppendLine($"Première modif personnage: {firstPersonMod?.DateModification:dd/MM/yyyy}");
        report.AppendLine($"Première modif Lucie Max (entité -2): {firstPieceMod?.DateModification:dd/MM/yyyy}");
        report.AppendLine();
    }

    private static void AppendTimelineComparison(ApplicationDbContext context, StatistiquesService service, StringBuilder report)
    {
        var selectedTimeline = service.GetSelectedTeamPowerEvolutionData();
        var bestTimeline = service.GetBestTeamPowerEvolutionData();

        report.AppendLine($"POINTS CALCULÉS:");
        report.AppendLine($"Équipe sélectionnée: {selectedTimeline.Count} points");
        report.AppendLine($"Meilleure équipe: {bestTimeline.Count} points");
        report.AppendLine();

        report.AppendLine("=== COMPARAISON DÉTAILLÉE PAR DATE ===");
        report.AppendLine();

        var classements = context.HistoriquesClassement.OrderBy(h => h.DateEnregistrement).ToList();
        var allDates = selectedTimeline.Select(p => p.Date)
            .Union(bestTimeline.Select(p => p.Date))
            .OrderBy(d => d)
            .ToList();

        foreach (var date in allDates)
            AppendDateComparison(context, selectedTimeline, bestTimeline, classements.Cast<dynamic>().ToList(), date, report);
    }

    private static void AppendDateComparison(ApplicationDbContext context, IEnumerable<dynamic> selectedTimeline, IEnumerable<dynamic> bestTimeline, List<dynamic> classements, DateOnly date, StringBuilder report)
    {
        var selectedPoint = selectedTimeline.FirstOrDefault(p => p.Date == date);
        var bestPoint = bestTimeline.FirstOrDefault(p => p.Date == date);
        var selectedPower = selectedPoint?.TotalPower ?? 0;
        var bestPower = bestPoint?.TotalPower ?? 0;
        var diff = bestPower - selectedPower;
        var diffIndicator = GetDiffIndicator(diff);

        report.AppendLine($"📅 {date:dd/MM/yyyy (ddd)}");
        report.AppendLine($"   Sélectionnée: {selectedPower,6} | Meilleure: {bestPower,6} | Écart: {diff,6} {diffIndicator}");

        AppendModificationsForDate(context, date, report);
        AppendClassementForDate(classements, date, report);
        report.AppendLine();
    }

    private static void AppendModificationsForDate(ApplicationDbContext context, DateOnly date, StringBuilder report)
    {
        var modsThisDay = context.HistoriquesModifications
            .Where(h => h.DateModification.Date == date.ToDateTime(TimeOnly.MinValue))
            .OrderBy(h => h.DateModification)
            .ToList();

        if (modsThisDay.Any())
        {
            report.AppendLine($"   Modifications ({modsThisDay.Count}):");
            foreach (var mod in modsThisDay.Take(10))
            {
                var entityType = mod.TypeEntite == TypeEntite.Personnage ? "Perso" : "Piece";
                var entityName = GetEntityName(mod);
                report.AppendLine($"     • {entityType} {entityName}: {mod.ChampModifie} {mod.AncienneValeur}→{mod.NouvelleValeur}");
            }
            if (modsThisDay.Count > 10)
                report.AppendLine($"     ... et {modsThisDay.Count - 10} autres");
        }
    }

    private static void AppendClassementForDate(List<dynamic> classements, DateOnly date, StringBuilder report)
    {
        var classementThisDay = classements.FirstOrDefault(c => c.DateEnregistrement == date);
        if (classementThisDay != null)
        {
            report.AppendLine($"   📊 Classement enregistré: {classementThisDay.PuissanceTotale}");
            report.AppendLine($"      Commandant: {classementThisDay.PuissanceCommandant} | Mercenaires: {classementThisDay.PuissanceMercenaires} | Lucie: {classementThisDay.PuissanceLucie}");
        }
    }

    private static void AppendCurrentValues(PersonnageService personnageService, StringBuilder report)
    {
        report.AppendLine("=== VALEURS ACTUELLES (LIVE) ===");
        var currentSelected = personnageService.GetPuissanceEscouade();
        var currentBest = personnageService.GetPuissanceMaxEscouade();
        var currentLucieSelected = personnageService.GetPuissanceLucieEscouade();
        var currentLucieMax = personnageService.GetPuissanceMaxLucieEscouade();

        report.AppendLine($"Équipe sélectionnée actuelle: {currentSelected}");
        report.AppendLine($"Meilleure équipe actuelle: {currentBest}");
        report.AppendLine($"Lucie sélectionnée: {currentLucieSelected}");
        report.AppendLine($"Lucie Max: {currentLucieMax}");
        report.AppendLine();
    }

    private static void AppendTopCharacters(PersonnageService personnageService, StringBuilder report)
    {
        var topMercs = personnageService.GetTopMercenaires().ToList();
        var topAndroids = personnageService.GetTopAndroides().ToList();
        var topCmd = personnageService.GetTopCommandant();

        report.AppendLine("Top 8 Mercenaires:");
        foreach (var m in topMercs)
            report.AppendLine($"  {m.Nom}: {m.Puissance} (Niveau {m.Niveau}, Rang {m.Rang})");
        
        report.AppendLine("Top 3 Androïdes:");
        foreach (var a in topAndroids)
            report.AppendLine($"  {a.Nom}: {a.Puissance}");

        if (topCmd != null)
            report.AppendLine($"Commandant: {topCmd.Nom}: {topCmd.Puissance} + {topCmd.Rang * 20} (rang) = {topCmd.Puissance + topCmd.Rang * 20}");
    }

    private static string GetEntityName(dynamic mod)
    {
        if (mod.EntiteId == -1)
            return "Lucie Select";
        if (mod.EntiteId == -2)
            return "Lucie Max";
        return mod.NomEntite;
    }

    private static string GetDiffIndicator(int diff)
    {
        if (diff > 0)
            return "✓";
        if (diff < 0)
            return "⚠";
        return "=";
    }
}
