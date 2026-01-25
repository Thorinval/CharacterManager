using CharacterManager.Server.Models;
using CharacterManager.Server.Services;
using CharacterManager.Server.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Components.Pages;

public partial class Statistiques : IAsyncDisposable
{
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = null!;
    
    [Inject]
    public StatistiquesService StatistiquesService { get; set; } = null!;

    private IJSObjectReference? chartModule;
    
    private const string ColorPrimaryPurple = "#667eea";
    private const string ColorSecondaryPurple = "#764ba2";
    private const string ColorAccentPink = "#f093fb";

    protected override void OnInitialized()
    {
        // Charger les mercenaires
        mercenaires = PersonnageService.GetMercenaires(false);

        if (mercenaires != null && mercenaires.Any())
        {
            // Calculer les statistiques
            statsTypeAttaque = CalculerStatistiquesParTypeAttaque(mercenaires);
            statsFaction = CalculerStatistiquesParFaction(mercenaires);
            statsRang = CalculerStatistiquesParRang(mercenaires);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && mercenaires != null && mercenaires.Any())
        {
            try
            {
                // Charger le module Chart.js
                chartModule = await JSRuntime.InvokeAsync<IJSObjectReference>(
                    "import", "/js/charts.js");

                // Créer le graphique des types d'attaque
                var labelsTypeAttaque = statsTypeAttaque.Keys.Select(k => GetTypeAttaqueLabel(k)).ToArray();
                var dataTypeAttaque = statsTypeAttaque.Values.ToArray();
                var colorsTypeAttaque = new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0" };

                await chartModule.InvokeVoidAsync("createPieChart", 
                    "chartTypeAttaque", 
                    labelsTypeAttaque, 
                    dataTypeAttaque,
                    colorsTypeAttaque);

                // Créer le graphique des factions
                var labelsFaction = statsFaction.Keys.Select(k => GetFactionLabel(k)).ToArray();
                var dataFaction = statsFaction.Values.ToArray();
                var colorsFaction = new[] { "#9966FF", "#FF9F40", "#4BC0C0" };

                await chartModule.InvokeVoidAsync("createPieChart", 
                    "chartFaction", 
                    labelsFaction, 
                    dataFaction,
                    colorsFaction);

                // Créer le graphique des rangs
                var labelsRang = statsRang.Keys.OrderByDescending(k => k).Select(k => $"Rang {k}").ToArray();
                var dataRang = statsRang.OrderByDescending(kvp => kvp.Key).Select(kvp => kvp.Value).ToArray();
                var colorsRang = new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0", "#9966FF" };

                await chartModule.InvokeVoidAsync("createPieChart", 
                    "chartRang", 
                    labelsRang, 
                    dataRang,
                    colorsRang);

                // Créer le graphique d'évolution des niveaux
                await InitializeLevelEvolutionChart();

                // Créer le graphique d'évolution de la puissance des mercenaires
                await InitializePowerEvolutionChart();

                // Créer le graphique d'évolution des classements
                await InitializeClassementEvolutionChart();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la création des graphiques: {ex.Message}");
            }
        }
    }

    private async Task InitializeLevelEvolutionChart()
    {
        if (mercenaires == null || !mercenaires.Any())
            return;

        try
        {
            var dailyData = StatistiquesService.GetLevelEvolutionData();
            if (!dailyData.Any())
                return;

            var labels = dailyData.Select(d => StatistiquesService.FormatDateWithDay(d.Date)).ToList();
            var personnagesAvecHistorique = StatistiquesService.GetPersonnagesWithHistory(dailyData);

            if (!personnagesAvecHistorique.Any())
                return;

            var datasets = StatistiquesService.CreateChartDatasets(dailyData, personnagesAvecHistorique, out int minLevel);

            await chartModule!.InvokeVoidAsync("createLineChart", "chartLevelEvolution", labels, datasets, 
                new { showDayNumbers = true, minLevel = minLevel });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la création du graphique d'évolution: {ex.Message}");
        }
    }

    private async Task InitializePowerEvolutionChart()
    {
        try
        {
            // Récupérer les données de l'équipe sélectionnée et de la meilleure équipe
            var selectedTeamData = StatistiquesService.GetSelectedTeamPowerEvolutionData();
            var bestTeamData = StatistiquesService.GetBestTeamPowerEvolutionData();

            // Fusionner les dates et créer les labels
            var allDates = selectedTeamData.Select(d => d.Date)
                .Union(bestTeamData.Select(d => d.Date))
                .OrderBy(d => d)
                .ToList();

            if (!allDates.Any())
                return;

            var labels = allDates.Select(d => StatistiquesService.FormatDateForClassement(d)).ToList();

            // Créer les datasets pour les deux courbes
            var datasets = new List<object>();

            // Courbe 1: Équipe sélectionnée (historique complet)
            var selectedTeamPowerData = allDates.Select(date =>
            {
                var record = selectedTeamData.FirstOrDefault(d => d.Date == date);
                return record != null ? (object)record.TotalPower : null;
            }).ToList();

            datasets.Add(new
            {
                label = LocalizationService.GetKeyValue("statistics.selectedTeamPower"),
                data = selectedTeamPowerData,
                borderColor = ColorPrimaryPurple,
                backgroundColor = StatistiquesService.ColorWithAlpha(ColorPrimaryPurple, 0.1),
                borderWidth = 2,
                fill = false,
                spanGaps = true,
                pointRadius = 3,
                pointHoverRadius = 5,
                tension = 0.3
            });

            // Courbe 2: Meilleure équipe (uniquement aujourd'hui)
            if (bestTeamData.Any())
            {
                var bestTeamPowerData = allDates.Select(date =>
                {
                    var record = bestTeamData.FirstOrDefault(d => d.Date == date);
                    return record != null ? (object)record.TotalPower : null;
                }).ToList();

                datasets.Add(new
                {
                    label = LocalizationService.GetKeyValue("statistics.bestTeamPower"),
                    data = bestTeamPowerData,
                    borderColor = ColorAccentPink,
                    backgroundColor = StatistiquesService.ColorWithAlpha(ColorAccentPink, 0.1),
                    borderWidth = 2,
                    fill = false,
                    spanGaps = true,
                    pointRadius = 3,
                    pointHoverRadius = 5,
                    tension = 0.3
                });
            }

            await chartModule!.InvokeVoidAsync("createLineChart", "chartPowerEvolution", labels, 
                datasets, new { showDayNumbers = false });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la création du graphique de puissance: {ex.Message}");
        }
    }

    private async Task InitializeClassementEvolutionChart()
    {
        try
        {
            var dailyData = StatistiquesService.GetClassementEvolutionData();
            if (!dailyData.Any())
                return;

            var labels = dailyData.Select(d => StatistiquesService.FormatDateForClassement(d.Date)).ToList();

            // Préparer les datasets pour chaque type de classement
            var datasets = new List<object>();
            var classementTypes = new[] { TypeClassement.Nutaku, TypeClassement.Top150, TypeClassement.France };
            var colors = new[] { ColorPrimaryPurple, ColorSecondaryPurple, ColorAccentPink };

            for (int i = 0; i < classementTypes.Length; i++)
            {
                var type = classementTypes[i];
                var data = dailyData.Select(d => (object)(d.Classements.ContainsKey(type) ? d.Classements[type] : 0)).ToList();

                datasets.Add(new
                {
                    label = GetClassementTypeLabel(type),
                    data = data,
                    backgroundColor = colors[i],
                    borderColor = colors[i],
                    borderWidth = 1
                });
            }

            await chartModule!.InvokeVoidAsync("createBarChart", "chartClassementEvolution", labels, datasets);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la création du graphique de classement: {ex.Message}");
        }
    }

    #region Méthodes d'étiquetage (non-métier)

    private string GetClassementTypeLabel(TypeClassement type) => type switch
    {
        TypeClassement.Nutaku => this.LocalizationService.GetKeyValue("statistics.classementNutaku"),
        TypeClassement.Top150 => this.LocalizationService.GetKeyValue("statistics.classementTop150"),
        TypeClassement.France => this.LocalizationService.GetKeyValue("statistics.classementFrance"),
        _ => "Unknown"
    };

    private static Dictionary<TypeAttaque, int> CalculerStatistiquesParTypeAttaque(IEnumerable<Personnage> mercenaires)
    {
        return mercenaires
            .Where(m => m.TypeAttaque != TypeAttaque.Inconnu)
            .GroupBy(m => m.TypeAttaque)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static Dictionary<Faction, int> CalculerStatistiquesParFaction(IEnumerable<Personnage> mercenaires)
    {
        return mercenaires
            .Where(m => m.Faction != Faction.Inconnu)
            .GroupBy(m => m.Faction)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static Dictionary<int, int> CalculerStatistiquesParRang(IEnumerable<Personnage> mercenaires)
    {
        return mercenaires
            .Where(m => m.Rang > 0)
            .GroupBy(m => m.Rang)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    internal string GetTypeAttaqueLabel(TypeAttaque typeAttaque) => typeAttaque switch
    {
        TypeAttaque.Melee => this.LocalizationService.GetKeyValue("home.attackType.melee"),
        TypeAttaque.Distance => this.LocalizationService.GetKeyValue("home.attackType.ranged"),
        TypeAttaque.Androide => this.LocalizationService.GetKeyValue("home.attackType.android"),
        TypeAttaque.Commandant => this.LocalizationService.GetKeyValue("home.attackType.commander"),
        _ => this.LocalizationService.GetKeyValue("home.attackType.unknown")
    };

    internal string GetFactionLabel(Faction faction) => faction switch
    {
        Faction.Syndicat => this.LocalizationService.GetKeyValue("home.faction.syndicat"),
        Faction.Pacificateurs => this.LocalizationService.GetKeyValue("home.faction.pacificateurs"),
        Faction.HommesLibres => this.LocalizationService.GetKeyValue("home.faction.hommesLibres"),
        _ => this.LocalizationService.GetKeyValue("home.faction.inconnu")
    };

    #endregion

    public async ValueTask DisposeAsync()
    {
        if (chartModule != null)
        {
            try
            {
                await chartModule.DisposeAsync();
            }
            catch
            {
                // Ignorer les erreurs de disposal
            }
        }
    }
}


