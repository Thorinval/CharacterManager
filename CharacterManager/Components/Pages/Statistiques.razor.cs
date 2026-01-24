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
    public ApplicationDbContext DbContext { get; set; } = null!;

    private IJSObjectReference? chartModule;

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
            var dailyData = GetLevelEvolutionData();
            if (!dailyData.Any())
                return;

            // Créer les labels avec le numéro du jour et le mois
            var labels = dailyData.Select(d => FormatDateWithDay(d.Date)).ToList();
            
            // Récupérer tous les noms uniques de personnages (seulement ceux avec historique)
            // En s'assurant qu'ils ont au moins une valeur non-zéro
            var allPersonnages = dailyData
                .SelectMany(w => w.LevelsByPersonnage.Keys)
                .Distinct()
                .OrderBy(n => n)
                .ToList();

            // Filtrer pour ne garder que les personnages qui ont réellement des données d'historique
            var personnagesAvecHistorique = new List<string>();
            foreach (var personnage in allPersonnages)
            {
                var donnees = dailyData.Select(w => 
                    w.LevelsByPersonnage.ContainsKey(personnage) ? w.LevelsByPersonnage[personnage] : 0
                ).ToList();
                
                // Vérifier que le personnage a au moins une donnée non-zéro
                if (donnees.Any(d => d > 0))
                {
                    personnagesAvecHistorique.Add(personnage);
                }
            }

            if (!personnagesAvecHistorique.Any())
                return;

            // Générer les couleurs pour les courbes
            var colors = GenerateColors(personnagesAvecHistorique.Count);
            
            var datasets = new List<object>();
            int minLevel = int.MaxValue;
            
            for (int i = 0; i < personnagesAvecHistorique.Count; i++)
            {
                var personnageName = personnagesAvecHistorique[i];
                var data = dailyData.Select(w => 
                    w.LevelsByPersonnage.ContainsKey(personnageName) ? w.LevelsByPersonnage[personnageName] : 0
                ).ToList();

                // Calculer le minimum parmi les valeurs non-zéro
                var nonZeroValues = data.Where(d => d > 0).ToList();
                if (nonZeroValues.Any())
                {
                    minLevel = Math.Min(minLevel, nonZeroValues.Min());
                }

                datasets.Add(new
                {
                    label = personnageName,
                    data = data,
                    borderColor = colors[i],
                    backgroundColor = ColorWithAlpha(colors[i], 0.1),
                    borderWidth = 2,
                    fill = false,
                    spanGaps = true,
                    pointRadius = 3,
                    pointHoverRadius = 5,
                    tension = 0.3
                });
            }

            // Si aucun minimum n'a été trouvé, utiliser 0
            if (minLevel == int.MaxValue)
                minLevel = 0;

            // Passer les informations des graduations journalières et du minimum à JavaScript
            await chartModule!.InvokeVoidAsync("createLineChart", "chartLevelEvolution", labels, datasets, 
                new { showDayNumbers = true, minLevel = minLevel });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la création du graphique d'évolution: {ex.Message}");
        }
    }

    private List<LevelEvolutionData> GetLevelEvolutionData()
    {
        var result = new List<LevelEvolutionData>();
        
        if (mercenaires == null || !mercenaires.Any() || DbContext == null)
            return result;

        // Créer un set des noms de mercenaires pour filtrage rapide
        var mercenairesNames = mercenaires.Select(m => m.Nom).ToHashSet();

        // Récupérer l'historique complet des modifications de niveau
        var allHistories = DbContext.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage && h.ChampModifie == "Niveau")
            .OrderBy(h => h.DateModification)
            .ToList();

        if (!allHistories.Any())
            return result;

        // Grouper par jour, en filtrant pour ne garder que les mercenaires
        var grouped = new Dictionary<DateTime, Dictionary<string, int>>();
        
        // Ajouter l'historique groupé par jour (uniquement les mercenaires)
        foreach (var history in allHistories)
        {
            // Vérifier que c'est un mercenaire
            if (!mercenairesNames.Contains(history.NomEntite))
                continue;

            var dayStart = history.DateModification.Date;
            if (!grouped.ContainsKey(dayStart))
                grouped[dayStart] = new Dictionary<string, int>();
            
            // Extraire la valeur numérique du niveau
            if (int.TryParse(history.NouvelleValeur, out var level))
            {
                grouped[dayStart][history.NomEntite] = level;
            }
        }

        // Créer une entrée pour chaque jour qui contient au moins une modification
        var sortedDays = grouped.Keys.OrderBy(d => d).ToList();
        foreach (var day in sortedDays)
        {
            var dayData = new Dictionary<string, int>();
            
            // Ajouter uniquement les modifications du jour pour ce jour
            foreach (var kvp in grouped[day])
            {
                dayData[kvp.Key] = kvp.Value;
            }

            result.Add(new LevelEvolutionData
            {
                Date = day,
                LevelsByPersonnage = dayData
            });
        }

        return result;
    }

    private string FormatDateWithDay(DateTime date)
    {
        var monthNames = new[] { "JAN", "FEV", "MAR", "AVR", "MAI", "JUN", "JUL", "AOU", "SEP", "OCT", "NOV", "DEC" };
        var month = monthNames[date.Month - 1];
        return $"{date.Day:D2} {month}";
    }

    private DateTime GetWeekStart(DateTime date)
    {
        var diff = date.DayOfWeek - DayOfWeek.Monday;
        if (diff < 0)
            diff += 7;
        return date.AddDays(-diff).Date;
    }

    private string FormatWeekLabel(DateTime weekStart)
    {
        var monthNames = new[] { "JAN", "FEV", "MAR", "AVR", "MAI", "JUN", "JUL", "AOU", "SEP", "OCT", "NOV", "DEC" };
        var month = monthNames[weekStart.Month - 1];
        var year = (weekStart.Year % 100).ToString("D2");
        return $"{month} {year}";
    }

    private List<string> GenerateColors(int count)
    {
        var baseColors = new[]
        {
            "#667eea", "#764ba2", "#f093fb", "#4facfe", "#00f2fe", "#43e97b", 
            "#fa709a", "#fee140", "#30cfd0", "#330867", "#ff006e", "#fb5607",
            "#ffbe0b", "#8338ec", "#3a86ff", "#06ffa5"
        };

        var colors = new List<string>();
        for (int i = 0; i < count; i++)
        {
            colors.Add(baseColors[i % baseColors.Length]);
        }
        return colors;
    }

    private string ColorWithAlpha(string hexColor, double alpha)
    {
        // Convert hex color to rgba
        var hex = hexColor.TrimStart('#');
        if (hex.Length == 6)
        {
            var r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            var g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            var b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
            return $"rgba({r},{g},{b},{alpha})";
        }
        return hexColor;
    }

    class LevelEvolutionData
    {
        public DateTime Date { get; set; }
        public Dictionary<string, int> LevelsByPersonnage { get; set; } = new();
    }

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

    internal  string GetTypeAttaqueLabel(TypeAttaque typeAttaque) => typeAttaque switch
    {
        TypeAttaque.Melee => this.LocalizationService.GetKeyValue("home.attackType.melee"),
        TypeAttaque.Distance => this.LocalizationService.GetKeyValue("home.attackType.ranged"),
        TypeAttaque.Androide => this.LocalizationService.GetKeyValue("home.attackType.android"),
        TypeAttaque.Commandant => this.LocalizationService.GetKeyValue("home.attackType.commander"),
        _ => this.LocalizationService.GetKeyValue("home.attackType.unknown")
    };

    internal  string GetFactionLabel(Faction faction) => faction switch
    {
        Faction.Syndicat => this.LocalizationService.GetKeyValue("home.faction.syndicat"),
        Faction.Pacificateurs => this.LocalizationService.GetKeyValue("home.faction.pacificateurs"),
        Faction.HommesLibres => this.LocalizationService.GetKeyValue("home.faction.hommesLibres"),
        _ => this.LocalizationService.GetKeyValue("home.faction.inconnu")
    };

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
