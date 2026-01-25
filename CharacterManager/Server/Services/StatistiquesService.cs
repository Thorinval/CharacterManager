using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using CharacterManager.Server.Constants;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Server.Services;

public class StatistiquesService : IStatistiquesService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPersonnageService _personnageService;

    public StatistiquesService(ApplicationDbContext dbContext, IPersonnageService personnageService)
    {
        _dbContext = dbContext;
        _personnageService = personnageService;
    }

    #region Statistiques de niveau

    public List<LevelEvolutionData> GetLevelEvolutionData()
    {
        var result = new List<LevelEvolutionData>();
        
        var mercenaires = _personnageService.GetMercenaires(false);
        if (mercenaires == null || !mercenaires.Any())
            return result;

        // Récupérer l'historique complet des modifications de niveau pour les mercenaires uniquement
        // en faisant une jointure avec la table Personnages pour vérifier le type
        var allHistories = _dbContext.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage && h.ChampModifie == StatisticsConstants.HistoryFields.Niveau)
            .Join(_dbContext.Personnages,
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

    public static List<string> GetPersonnagesWithHistory(List<LevelEvolutionData> dailyData)
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

    public static List<object> CreateChartDatasets(List<LevelEvolutionData> dailyData, List<string> personnages, out int minLevel)
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

    #endregion

    #region Statistiques de classement

    public List<ClassementEvolutionData> GetClassementEvolutionData()
    {
        var result = new List<ClassementEvolutionData>();
        
        // Récupérer l'historique des classements triés par date
        var allHistories = _dbContext.HistoriquesClassement
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

    #endregion

    #region Statistiques de puissance

    public List<TeamPowerEvolutionData> GetSelectedTeamPowerEvolutionData()
    {
        var result = new List<TeamPowerEvolutionData>();

        var premierClassement = _dbContext.HistoriquesClassement
            .OrderBy(h => h.DateEnregistrement)
            .FirstOrDefault();

        if (premierClassement == null)
            return result;

        result.Add(new TeamPowerEvolutionData
        {
            Date = premierClassement.DateEnregistrement,
            TotalPower = premierClassement.PuissanceTotale
        });

        var dateDebut = premierClassement.DateEnregistrement.ToDateTime(TimeOnly.MinValue);
        var personnagesSelectionnes = GetInitialSelectedPersonnages(dateDebut);
        int puissanceActuelle = premierClassement.PuissanceTotale;

        var modifications = _dbContext.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage && h.DateModification > dateDebut)
            .OrderBy(h => h.DateModification)
            .ToList();

        var modificationsParDate = modifications
            .GroupBy(m => DateOnly.FromDateTime(m.DateModification))
            .OrderBy(g => g.Key);

        ProcessSelectedTeamModifications(modificationsParDate, personnagesSelectionnes, ref puissanceActuelle, result);
        AddOtherClassements(premierClassement.DateEnregistrement, result);
        AddCurrentSelectedPower(result);

        return result.OrderBy(r => r.Date).ToList();
    }

    private void ProcessSelectedTeamModifications(
        IOrderedEnumerable<IGrouping<DateOnly, HistoriqueModification>> modificationsParDate,
        HashSet<int> personnagesSelectionnes,
        ref int puissanceActuelle,
        List<TeamPowerEvolutionData> result)
    {
        foreach (var groupe in modificationsParDate)
        {
            var (selectionModifiee, puissanceModifiee, updatedPower) = ProcessSelectedTeamGroup(groupe, personnagesSelectionnes, puissanceActuelle);
            puissanceActuelle = updatedPower;

            if (selectionModifiee || puissanceModifiee)
            {
                result.Add(new TeamPowerEvolutionData
                {
                    Date = groupe.Key,
                    TotalPower = puissanceActuelle
                });
            }
        }
    }

    private static (bool SelectionModifiee, bool PuissanceModifiee, int UpdatedPower) ProcessSelectedTeamGroup(
        IGrouping<DateOnly, HistoriqueModification> groupe,
        HashSet<int> personnagesSelectionnes,
        int puissanceActuelle)
    {
        bool selectionModifiee = false;
        bool puissanceModifiee = false;

        foreach (var modif in groupe)
        {
            if (modif.ChampModifie == StatisticsConstants.HistoryFields.Selectionne)
            {
                selectionModifiee = true;
                UpdateSelectionState(modif, personnagesSelectionnes);
            }

            if (modif.ChampModifie == StatisticsConstants.HistoryFields.Puissance && personnagesSelectionnes.Contains(modif.EntiteId))
            {
                puissanceModifiee = true;
                puissanceActuelle = UpdateTeamPower(modif, personnagesSelectionnes, puissanceActuelle);
            }
        }

        return (selectionModifiee, puissanceModifiee, puissanceActuelle);
    }

    private void AddOtherClassements(DateOnly premierClassementDate, List<TeamPowerEvolutionData> result)
    {
        var autresClassements = _dbContext.HistoriquesClassement
            .Where(h => h.DateEnregistrement > premierClassementDate)
            .OrderBy(h => h.DateEnregistrement)
            .ToList();

        foreach (var classement in autresClassements.Where(c => !result.Any(r => r.Date == c.DateEnregistrement)))
        {
            result.Add(new TeamPowerEvolutionData
            {
                Date = classement.DateEnregistrement,
                TotalPower = classement.PuissanceTotale
            });
        }
    }

    private void AddCurrentSelectedPower(List<TeamPowerEvolutionData> result)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (!result.Any(r => r.Date == today))
        {
            var currentPower = _personnageService.GetPuissanceEscouade();
            if (currentPower > 0)
            {
                result.Add(new TeamPowerEvolutionData
                {
                    Date = today,
                    TotalPower = currentPower
                });
            }
        }
    }

    public List<TeamPowerEvolutionData> GetBestTeamPowerEvolutionData()
    {
        var result = new List<TeamPowerEvolutionData>();

        var premierClassement = _dbContext.HistoriquesClassement
            .OrderBy(h => h.DateEnregistrement)
            .FirstOrDefault();

        if (premierClassement == null)
            return result;

        result.Add(new TeamPowerEvolutionData
        {
            Date = premierClassement.DateEnregistrement,
            TotalPower = premierClassement.PuissanceTotale
        });

        var dateDebut = premierClassement.DateEnregistrement.ToDateTime(TimeOnly.MinValue);
        var personnagesSelectionnes = GetInitialSelectedPersonnages(dateDebut);
        int puissanceEquipe = premierClassement.PuissanceTotale;

        var modifications = _dbContext.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage && h.DateModification > dateDebut)
            .OrderBy(h => h.DateModification)
            .ToList();

        var modificationsParDate = modifications
            .GroupBy(m => DateOnly.FromDateTime(m.DateModification))
            .OrderBy(g => g.Key);

        ProcessDailyModifications(modificationsParDate, personnagesSelectionnes, ref puissanceEquipe, result);
        AddCurrentBestPower(result);

        return result.OrderBy(r => r.Date).ToList();
    }

    private HashSet<int> GetInitialSelectedPersonnages(DateTime dateDebut)
    {
        var personnagesSelectionnes = new HashSet<int>(
            _dbContext.Personnages
                .Where(p => p.Selectionne)
                .Select(p => p.Id)
        );
        
        var modificationsAntérieures = _dbContext.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage 
                     && h.ChampModifie == StatisticsConstants.HistoryFields.Selectionne
                     && h.DateModification <= dateDebut)
            .OrderByDescending(h => h.DateModification)
            .ToList();

        foreach (var modif in modificationsAntérieures)
        {
            if (modif.AncienneValeur?.ToLower() == AppConstants.BooleanStrings.True)
                personnagesSelectionnes.Add(modif.EntiteId);
            else
                personnagesSelectionnes.Remove(modif.EntiteId);
        }

        return personnagesSelectionnes;
    }

    private void ProcessDailyModifications(IOrderedEnumerable<IGrouping<DateOnly, HistoriqueModification>> modificationsParDate, 
        HashSet<int> personnagesSelectionnes, ref int puissanceEquipe, List<TeamPowerEvolutionData> result)
    {
        foreach (var groupe in modificationsParDate)
        {
            var (needsRecalculation, updatedPower) = ProcessModificationGroup(groupe, personnagesSelectionnes, puissanceEquipe);
            puissanceEquipe = updatedPower;

            var puissanceMax = needsRecalculation 
                ? _personnageService.GetPuissanceMaxEscouade() 
                : puissanceEquipe;

            if (result[result.Count - 1].TotalPower != puissanceMax)
            {
                result.Add(new TeamPowerEvolutionData
                {
                    Date = groupe.Key,
                    TotalPower = puissanceMax
                });
            }
        }
    }

    private static (bool NeedsRecalculation, int UpdatedPower) ProcessModificationGroup(
        IGrouping<DateOnly, HistoriqueModification> groupe, 
        HashSet<int> personnagesSelectionnes, 
        int puissanceEquipe)
    {
        bool recalculerMax = false;

        foreach (var modif in groupe)
        {
            UpdateSelectionState(modif, personnagesSelectionnes);
            puissanceEquipe = UpdateTeamPower(modif, personnagesSelectionnes, puissanceEquipe);
            
            if (ShouldRecalculateMaxPower(modif, personnagesSelectionnes))
                recalculerMax = true;
        }

        return (recalculerMax, puissanceEquipe);
    }

    private static void UpdateSelectionState(HistoriqueModification modif, HashSet<int> personnagesSelectionnes)
    {
        if (modif.ChampModifie == StatisticsConstants.HistoryFields.Selectionne)
        {
            if (modif.NouvelleValeur?.ToLower() == AppConstants.BooleanStrings.True)
                personnagesSelectionnes.Add(modif.EntiteId);
            else
                personnagesSelectionnes.Remove(modif.EntiteId);
        }
    }

    private static int UpdateTeamPower(HistoriqueModification modif, HashSet<int> personnagesSelectionnes, int puissanceEquipe)
    {
        if (modif.ChampModifie == StatisticsConstants.HistoryFields.Puissance 
            && personnagesSelectionnes.Contains(modif.EntiteId) 
            && int.TryParse(modif.AncienneValeur, out int anciennePuissance)
            && int.TryParse(modif.NouvelleValeur, out int nouvellePuissance))
        {
            return puissanceEquipe + (nouvellePuissance - anciennePuissance);
        }
        return puissanceEquipe;
    }

    private static bool ShouldRecalculateMaxPower(HistoriqueModification modif, HashSet<int> personnagesSelectionnes)
    {
        return (modif.ChampModifie == StatisticsConstants.HistoryFields.Puissance 
                || modif.ChampModifie == StatisticsConstants.HistoryFields.Niveau 
                || modif.ChampModifie == StatisticsConstants.HistoryFields.Rang) 
            && !personnagesSelectionnes.Contains(modif.EntiteId);
    }

    private void AddCurrentBestPower(List<TeamPowerEvolutionData> result)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        if (!result.Any(r => r.Date == today))
        {
            var currentBestPower = _personnageService.GetPuissanceMaxEscouade();
            if (currentBestPower > 0)
            {
                result.Add(new TeamPowerEvolutionData
                {
                    Date = today,
                    TotalPower = currentBestPower
                });
            }
        }
    }

    #endregion

    #region Utilitaires de formatage

    public static string FormatDateWithDay(DateTime date)
    {
        var month = StatisticsConstants.MonthNames.Values[date.Month - 1];
        return $"{date.Day:D2} {month}";
    }

    public static string FormatDateForClassement(DateOnly date)
    {
        var month = StatisticsConstants.MonthNames.Values[date.Month - 1];
        return $"{date.Day:D2} {month}";
    }

    public static string GetClassementTypeLabel(TypeClassement type, Func<string, string> localizationFunction) => type switch
    {
        TypeClassement.Nutaku => localizationFunction(StatisticsConstants.LocalizationKeys.ClassementNutaku),
        TypeClassement.Top150 => localizationFunction(StatisticsConstants.LocalizationKeys.ClassementTop150),
        TypeClassement.France => localizationFunction(StatisticsConstants.LocalizationKeys.ClassementFrance),
        _ => "Unknown"
    };

    public static List<string> GenerateColors(int count)
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

    public static string ColorWithAlpha(string hexColor, double alpha = StatisticsConstants.ChartFormatting.AlphaFill)
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

    #endregion

    #region Instance wrappers for static methods (for interface compliance)

    string IStatistiquesService.FormatDateWithDay(DateTime date) => FormatDateWithDay(date);
    string IStatistiquesService.FormatDateForClassement(DateOnly date) => FormatDateForClassement(date);
    string IStatistiquesService.ColorWithAlpha(string hexColor, double alpha) => ColorWithAlpha(hexColor, alpha);
    List<string> IStatistiquesService.GetPersonnagesWithHistory(List<LevelEvolutionData> dailyData) => GetPersonnagesWithHistory(dailyData);
    List<object> IStatistiquesService.CreateChartDatasets(List<LevelEvolutionData> dailyData, List<string> personnages, out int minLevel) => CreateChartDatasets(dailyData, personnages, out minLevel);

    #endregion
}

#region Data Classes

public class LevelEvolutionData
{
    public DateTime Date { get; set; }
    public Dictionary<string, int> LevelsByPersonnage { get; set; } = new();
}

public class ClassementEvolutionData
{
    public DateOnly Date { get; set; }
    public Dictionary<TypeClassement, int> Classements { get; set; } = new();
}

public class TeamPowerEvolutionData
{
    public DateOnly Date { get; set; }
    public int TotalPower { get; set; }
}

#endregion




