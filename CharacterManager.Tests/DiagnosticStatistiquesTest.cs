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
    private readonly ITestOutputHelper _output;

    public DiagnosticStatistiquesTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateDetailedPowerEvolutionReport()
    {
        // Utiliser la vraie base de données
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlite("Data Source=d:\\Devs\\CharacterManager\\CharacterManager\\charactermanager.db");
        
        var context = new ApplicationDbContext(optionsBuilder.Options);
        var historiqueService = new HistoriqueModificationService(context);
        var loggerMock = new Mock<ILogger<PersonnageService>>();
        var personnageService = new PersonnageService(context, historiqueService, loggerMock.Object);
        var service = new StatistiquesService(context, personnageService);

        var report = new StringBuilder();
        report.AppendLine("=== RAPPORT DÉTAILLÉ ÉVOLUTION PUISSANCE ===");
        report.AppendLine($"Généré le {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        report.AppendLine();

        // 1. Info sur les classements
        var classements = context.HistoriquesClassement
            .OrderBy(h => h.DateEnregistrement)
            .ToList();
        
        report.AppendLine($"CLASSEMENTS ENREGISTRÉS: {classements.Count}");
        report.AppendLine($"Premier: {classements.FirstOrDefault()?.DateEnregistrement:dd/MM/yyyy} - Puissance: {classements.FirstOrDefault()?.PuissanceTotale}");
        report.AppendLine($"Dernier: {classements.LastOrDefault()?.DateEnregistrement:dd/MM/yyyy} - Puissance: {classements.LastOrDefault()?.PuissanceTotale}");
        report.AppendLine();

        // 2. Info sur les modifications historiques
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

        // 3. Calculer les deux courbes
        var selectedTimeline = service.GetSelectedTeamPowerEvolutionData();
        var bestTimeline = service.GetBestTeamPowerEvolutionData();

        report.AppendLine($"POINTS CALCULÉS:");
        report.AppendLine($"Équipe sélectionnée: {selectedTimeline.Count} points");
        report.AppendLine($"Meilleure équipe: {bestTimeline.Count} points");
        report.AppendLine();

        // 4. Comparer les deux courbes point par point
        report.AppendLine("=== COMPARAISON DÉTAILLÉE PAR DATE ===");
        report.AppendLine();

        var allDates = selectedTimeline.Select(p => p.Date)
            .Union(bestTimeline.Select(p => p.Date))
            .OrderBy(d => d)
            .ToList();

        foreach (var date in allDates)
        {
            var selectedPoint = selectedTimeline.FirstOrDefault(p => p.Date == date);
            var bestPoint = bestTimeline.FirstOrDefault(p => p.Date == date);
            var selectedPower = selectedPoint?.TotalPower ?? 0;
            var bestPower = bestPoint?.TotalPower ?? 0;
            var diff = bestPower - selectedPower;

            report.AppendLine($"📅 {date:dd/MM/yyyy (ddd)}");
            report.AppendLine($"   Sélectionnée: {selectedPower,6} | Meilleure: {bestPower,6} | Écart: {diff,6} {(diff > 0 ? "✓" : diff < 0 ? "⚠" : "=")}");

            // Modifications ce jour-là
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
                    var entityName = mod.EntiteId == -1 ? "Lucie Select" : mod.EntiteId == -2 ? "Lucie Max" : mod.NomEntite;
                    report.AppendLine($"     • {entityType} {entityName}: {mod.ChampModifie} {mod.AncienneValeur}→{mod.NouvelleValeur}");
                }
                if (modsThisDay.Count > 10)
                    report.AppendLine($"     ... et {modsThisDay.Count - 10} autres");
            }

            // Classement ce jour-là
            var classementThisDay = classements.FirstOrDefault(c => c.DateEnregistrement == date);
            if (classementThisDay != null)
            {
                report.AppendLine($"   📊 Classement enregistré: {classementThisDay.PuissanceTotale}");
                report.AppendLine($"      Commandant: {classementThisDay.PuissanceCommandant} | Mercenaires: {classementThisDay.PuissanceMercenaires} | Lucie: {classementThisDay.PuissanceLucie}");
            }

            report.AppendLine();
        }

        // 5. Valeurs actuelles calculées
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

        // 6. Top personnages actuels
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

        // Afficher le rapport
        var reportText = report.ToString();
        _output.WriteLine(reportText);

        // Sauvegarder aussi dans un fichier
        var reportPath = Path.Combine("d:\\Devs\\CharacterManager", $"diagnostic_power_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(reportPath, reportText);
        _output.WriteLine($"\n✅ Rapport sauvegardé: {reportPath}");

        context.Dispose();
    }
}
