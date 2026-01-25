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
            var dailyData = GetLevelEvolutionData();
            if (!dailyData.Any())
                return;

            var labels = dailyData.Select(d => FormatDateWithDay(d.Date)).ToList();
            var personnagesAvecHistorique = GetPersonnagesWithHistory(dailyData);

            if (!personnagesAvecHistorique.Any())
                return;

            var datasets = CreateChartDatasets(dailyData, personnagesAvecHistorique, out int minLevel);

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
            var dailyData = GetPowerEvolutionData();
            if (!dailyData.Any())
                return;

            var labels = dailyData.Select(d => FormatDateForClassement(d.Date)).ToList();
            var data = dailyData.Select(d => (object)d.PuissanceMercenaires).ToList();

            var dataset = new
            {
                label = LocalizationService.GetKeyValue("statistics.mercenairePower"),
                data = data,
                borderColor = ColorPrimaryPurple,
                backgroundColor = ColorWithAlpha(ColorPrimaryPurple, 0.1),
                borderWidth = 2,
                fill = true,
                spanGaps = true,
                pointRadius = 3,
                pointHoverRadius = 5,
                tension = 0.3
            };

            await chartModule!.InvokeVoidAsync("createLineChart", "chartPowerEvolution", labels, 
                new[] { dataset }, new { showDayNumbers = false });
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
            var dailyData = GetClassementEvolutionData();
            if (!dailyData.Any())
                return;

            var labels = dailyData.Select(d => FormatDateForClassement(d.Date)).ToList();

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
                    backgroundColor = ColorWithAlpha(colors[i], 0.7),
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

    private static List<string> GetPersonnagesWithHistory(List<LevelEvolutionData> dailyData)
    {
        var allPersonnages = dailyData
            .SelectMany(w => w.LevelsByPersonnage.Keys)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        var personnagesAvecHistorique = new List<string>();
        foreach (var personnage in allPersonnages)
        {
            // Vérifier qu'il y a au moins une donnée réelle pour ce personnage
            var aDonnees = dailyData.Any(w => 
                w.LevelsByPersonnage.ContainsKey(personnage) && w.LevelsByPersonnage[personnage] > 0
            );
            
            if (aDonnees)
            {
                personnagesAvecHistorique.Add(personnage);
            }
        }

        return personnagesAvecHistorique;
    }

    private static List<object> CreateChartDatasets(List<LevelEvolutionData> dailyData, List<string> personnages, out int minLevel)
    {
        var colors = GenerateColors(personnages.Count);
        var datasets = new List<object>();
        minLevel = int.MaxValue;
        
        for (int i = 0; i < personnages.Count; i++)
        {
            var personnageName = personnages[i];
            // Utiliser null au lieu de 0 pour les données manquantes
            var data = dailyData.Select(w => 
                w.LevelsByPersonnage.ContainsKey(personnageName) 
                    ? (object)w.LevelsByPersonnage[personnageName] 
                    : null
            ).ToList();

            // Trouver le niveau minimum parmi les valeurs non nulles
            var nonNullValues = data.Where(d => d != null).Cast<int>().ToList();
            if (nonNullValues.Any())
            {
                minLevel = Math.Min(minLevel, nonNullValues.Min());
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

        if (minLevel == int.MaxValue)
            minLevel = 0;

        return datasets;
    }

    private List<LevelEvolutionData> GetLevelEvolutionData()
    {
        var result = new List<LevelEvolutionData>();
        
        if (mercenaires == null || !mercenaires.Any() || DbContext == null)
            return result;

        // Récupérer l'historique complet des modifications de niveau pour les mercenaires uniquement
        // en faisant une jointure avec la table Personnages pour vérifier le type
        var allHistories = DbContext.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage && h.ChampModifie == "Niveau")
            .Join(DbContext.Personnages,
                h => h.EntiteId,
                p => p.Id,
                (h, p) => new { History = h, Personnage = p })
            .Where(x => x.Personnage.Type == TypePersonnage.Mercenaire)
            .Select(x => x.History)
            .OrderBy(h => h.DateModification)
            .ToList();

        if (!allHistories.Any())
            return result;

        // Grouper par jour
        var grouped = new Dictionary<DateTime, Dictionary<string, int>>();
        
        // Ajouter l'historique groupé par jour
        foreach (var history in allHistories)
        {
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

    private static string FormatDateWithDay(DateTime date)
    {
        var monthNames = new[] { "JAN", "FEV", "MAR", "AVR", "MAI", "JUN", "JUL", "AOU", "SEP", "OCT", "NOV", "DEC" };
        var month = monthNames[date.Month - 1];
        return $"{date.Day:D2} {month}";
    }

    private static string FormatDateForClassement(DateOnly date)
    {
        var monthNames = new[] { "JAN", "FEV", "MAR", "AVR", "MAI", "JUN", "JUL", "AOU", "SEP", "OCT", "NOV", "DEC" };
        var month = monthNames[date.Month - 1];
        return $"{date.Day:D2} {month}";
    }

    private string GetClassementTypeLabel(TypeClassement type) => type switch
    {
        TypeClassement.Nutaku => this.LocalizationService.GetKeyValue("statistics.classementNutaku"),
        TypeClassement.Top150 => this.LocalizationService.GetKeyValue("statistics.classementTop150"),
        TypeClassement.France => this.LocalizationService.GetKeyValue("statistics.classementFrance"),
        _ => "Unknown"
    };

    private static List<string> GenerateColors(int count)
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

    private static string ColorWithAlpha(string hexColor, double alpha)
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

    private List<PowerEvolutionData> GetPowerEvolutionData()
    {
        var result = new List<PowerEvolutionData>();
        
        if (DbContext == null)
            return result;

        // Récupérer l'historique des classements triés par date
        var allHistories = DbContext.HistoriquesClassement
            .OrderBy(h => h.DateEnregistrement)
            .ToList();

        if (!allHistories.Any())
            return result;

        // Créer une entrée pour chaque enregistrement de classement
        foreach (var history in allHistories)
        {
            result.Add(new PowerEvolutionData
            {
                Date = history.DateEnregistrement,
                PuissanceMercenaires = history.PuissanceMercenaires
            });
        }

        return result;
    }

    private List<ClassementEvolutionData> GetClassementEvolutionData()
    {
        var result = new List<ClassementEvolutionData>();
        
        if (DbContext == null)
            return result;

        // Récupérer l'historique des classements triés par date
        var allHistories = DbContext.HistoriquesClassement
            .Include(h => h.Classements)
            .OrderBy(h => h.DateEnregistrement)
            .ToList();

        if (!allHistories.Any())
            return result;

        // Créer une entrée pour chaque enregistrement de classement
        foreach (var history in allHistories)
        {
            var classementValues = new Dictionary<TypeClassement, int>();
            
            foreach (var classement in history.Classements)
            {
                classementValues[classement.Type] = classement.Valeur;
            }

            result.Add(new ClassementEvolutionData
            {
                Date = history.DateEnregistrement,
                Classements = classementValues
            });
        }

        return result;
    }

    class LevelEvolutionData
    {
        public DateTime Date { get; set; }
        public Dictionary<string, int> LevelsByPersonnage { get; set; } = new();
    }

    class PowerEvolutionData
    {
        public DateOnly Date { get; set; }
        public int PuissanceMercenaires { get; set; }
    }

    class ClassementEvolutionData
    {
        public DateOnly Date { get; set; }
        public Dictionary<TypeClassement, int> Classements { get; set; } = new();
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
