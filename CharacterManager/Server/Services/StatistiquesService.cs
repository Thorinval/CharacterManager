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
        // Try to return from materialized timeline first
        try
        {
            var cached = _dbContext.TeamPowerTimelineRecords
                .Where(r => r.Type == Models.Enums.TeamPowerTimelineType.Selected)
                .OrderBy(r => r.Date)
                .Select(r => new TeamPowerEvolutionData { Date = r.Date, TotalPower = r.TotalPower })
                .ToList();
            if (cached.Any())
                return cached;
        }
        catch
        {
            // Fallback to compute if table missing
        }

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

        // Récupérer les modifications affectant l'équipe sélectionnée
        var modifications = _dbContext.HistoriquesModifications
            .Where(h => (h.TypeEntite == TypeEntite.Personnage || 
                        (h.TypeEntite == TypeEntite.Piece && h.EntiteId == -1 && h.ChampModifie == StatisticsConstants.HistoryFields.PuissanceLucieSelectionnee))
                     && h.DateModification > dateDebut)
            .OrderBy(h => h.DateModification)
            .ToList();

        var modificationsParDate = modifications
            .GroupBy(m => DateOnly.FromDateTime(m.DateModification))
            .OrderBy(g => g.Key);

        // Pour chaque date de modification, recalculer complètement la puissance sélectionnée
        // en prenant en compte l'historique complet jusqu'à cette date, sans utiliser de deltas
        ProcessSelectedTeamDailyModifications(modificationsParDate, result);
        AddOtherClassements(premierClassement.DateEnregistrement, result);
        AddCurrentSelectedPower(result);

        return result.OrderBy(r => r.Date).ToList();
    }

    private void ProcessSelectedTeamDailyModifications(
        IOrderedEnumerable<IGrouping<DateOnly, HistoriqueModification>> modificationsParDate,
        List<TeamPowerEvolutionData> result)
    {
        foreach (var dateKey in modificationsParDate.Select(groupe => groupe.Key))
        {
            // Recalculer complètement l'équipe sélectionnée à cette date
            // en prenant les états actuels de chaque personnage et la puissance Lucie
            var dateTime = dateKey.ToDateTime(TimeOnly.MinValue);
            var puissanceSelectionnee = CalculateSelectedTeamPowerAtDate(dateTime);

            if (result.Count == 0 || result[result.Count - 1].TotalPower != puissanceSelectionnee)
            {
                result.Add(new TeamPowerEvolutionData
                {
                    Date = dateKey,
                    TotalPower = puissanceSelectionnee
                });
            }
        }
    }

    /// <summary>
    /// Calcule la puissance totale de l'équipe sélectionnée à une date donnée en prenant la dernière valeur connue de chaque personnage
    /// </summary>
    private int CalculateSelectedTeamPowerAtDate(DateTime dateTime)
    {
        // Appliquer les modifications de sélection jusqu'à cette date
        var modificationsSelection = _dbContext.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage 
                     && h.ChampModifie == StatisticsConstants.HistoryFields.Selectionne
                     && h.DateModification <= dateTime)
            .OrderByDescending(h => h.DateModification)
            .ToList();

        // Construire l'état de sélection à cette date
        var selectionStates = new Dictionary<int, bool>();
        foreach (var modif in modificationsSelection.Where(m => !selectionStates.ContainsKey(m.EntiteId)))
        {
            selectionStates[modif.EntiteId] = modif.NouvelleValeur?.ToLower() == AppConstants.BooleanStrings.True;
        }

        // Reconstruire l'ensemble des personnages sélectionnés à cette date
        var personnagesSelectionnésAtDate = new HashSet<int>();
        foreach (var p in _dbContext.Personnages.Where(x => x.GetType() == typeof(Personnage)).ToList())
        {
            if (selectionStates.ContainsKey(p.Id))
            {
                if (selectionStates[p.Id])
                    personnagesSelectionnésAtDate.Add(p.Id);
            }
            else if (p.Selectionne)
            {
                personnagesSelectionnésAtDate.Add(p.Id);
            }
        }

        // Calculer la puissance totale : personnages sélectionnés + puissance Lucie sélectionnée
        var puissancePersonnages = _dbContext.Personnages
            .Where(p => p.GetType() == typeof(Personnage) && personnagesSelectionnésAtDate.Contains(p.Id))
            .AsEnumerable()
            .Sum(p => GetPersonnagePuissanceAtDate(p.Id, dateTime));

        var puissanceLucie = GetSelectedLuciePowerAtDate(dateTime);

        return puissancePersonnages + puissanceLucie;
    }

    /// <summary>
    /// Récupère la puissance sélectionnée de Lucie à une date donnée (dernière valeur connue)
    /// </summary>
    private int GetSelectedLuciePowerAtDate(DateTime dateTime)
    {
        // Chercher l'historique des modifications de puissance sélectionnée de Lucie
        var historyAtDate = _dbContext.HistoriquesModifications
            .Where(h => h.EntiteId == -1 
                     && h.TypeEntite == TypeEntite.Piece
                     && h.ChampModifie == StatisticsConstants.HistoryFields.PuissanceLucieSelectionnee
                     && h.DateModification <= dateTime)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefault();

        if (historyAtDate != null && int.TryParse(historyAtDate.NouvelleValeur, out int selectedValue))
            return selectedValue;

        // Pas d'historique : retourner 0
        return 0;
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
        var currentPower = _personnageService.GetPuissanceEscouade();

        // Mettre à jour l'entrée du jour si elle existe, sinon l'ajouter
        var todayEntry = result.FirstOrDefault(r => r.Date == today);
        if (todayEntry != null)
        {
            todayEntry.TotalPower = currentPower;
        }
        else if (currentPower > 0)
        {
            result.Add(new TeamPowerEvolutionData
            {
                Date = today,
                TotalPower = currentPower
            });
        }
    }

    public List<TeamPowerEvolutionData> GetBestTeamPowerEvolutionData()
    {
        // Try to return from materialized timeline first
        try
        {
            var cached = _dbContext.TeamPowerTimelineRecords
                .Where(r => r.Type == Models.Enums.TeamPowerTimelineType.Best)
                .OrderBy(r => r.Date)
                .Select(r => new TeamPowerEvolutionData { Date = r.Date, TotalPower = r.TotalPower })
                .ToList();
            if (cached.Any())
                return cached;
        }
        catch
        {
            // Fallback to compute if table missing
        }

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

        // Récupérer les modifications des personnages ET de la puissance de Lucie max
        var modifications = _dbContext.HistoriquesModifications
            .Where(h => (h.TypeEntite == TypeEntite.Personnage || 
                        (h.TypeEntite == TypeEntite.Piece && h.EntiteId == -2 && h.ChampModifie == StatisticsConstants.HistoryFields.PuissanceLucieMax))
                     && h.DateModification > dateDebut)
            .OrderBy(h => h.DateModification)
            .ToList();

        var modificationsParDate = modifications
            .GroupBy(m => DateOnly.FromDateTime(m.DateModification))
            .OrderBy(g => g.Key);

        // Si aucun historique exploitable avant la première date d'historique, aligner la courbe "meilleure" sur l'équipe sélectionnée
        var earliestHistoryDate = modificationsParDate.Any() ? modificationsParDate.First().Key : (DateOnly?)null;
        if (earliestHistoryDate.HasValue)
        {
            var selectedTimeline = GetSelectedTeamPowerEvolutionData();
            var preHistoryPoints = selectedTimeline.Where(p => p.Date < earliestHistoryDate.Value).ToList();
            
            foreach (var point in preHistoryPoints)
            {
                var existing = result.FirstOrDefault(r => r.Date == point.Date);
                if (existing != null)
                {
                    existing.TotalPower = point.TotalPower;
                }
                else
                {
                    result.Add(new TeamPowerEvolutionData { Date = point.Date, TotalPower = point.TotalPower });
                }
            }
            
            result = result.OrderBy(r => r.Date).ToList();
        }

        ProcessDailyModifications(modificationsParDate, result);
        AddCurrentBestPower(result);

        return result.OrderBy(r => r.Date).ToList();
    }

    private void ProcessDailyModifications(IOrderedEnumerable<IGrouping<DateOnly, HistoriqueModification>> modificationsParDate, 
        List<TeamPowerEvolutionData> result)
    {
        foreach (var dateKey in modificationsParDate.Select(groupe => groupe.Key))
        {
            // Pour la meilleure équipe, toujours recalculer au lieu d'utiliser des deltas
            // car les modifications Lucie Max ne correspondent pas à la base sélectionnée
            var puissanceMax = CalculateBestTeamPowerAtDate(dateKey);

            if (result.Count == 0 || result[result.Count - 1].TotalPower != puissanceMax)
            {
                result.Add(new TeamPowerEvolutionData
                {
                    Date = dateKey,
                    TotalPower = puissanceMax
                });
            }
        }
    }

    /// <summary>
    /// Calcule la meilleure puissance d'équipe possible à une date donnée en utilisant l'historique
    /// </summary>
    private int CalculateBestTeamPowerAtDateForDateTime(DateTime dateTime, bool useCurrentState = false)
    {

        // Récupérer tous les personnages et leurs états à cette date
        var mercenairesAtDate = _dbContext.Personnages
            .Where(p => p.Type == TypePersonnage.Mercenaire && p.GetType() == typeof(Personnage))
            .AsEnumerable()
            .Select(p => new
            {
                Personnage = p,
                Puissance = GetPersonnagePuissanceAtDate(p.Id, dateTime),
                Niveau = GetPersonnagePropertyAtDate(p.Id, StatisticsConstants.HistoryFields.Niveau, dateTime, p.Niveau),
                Rang = GetPersonnagePropertyAtDate(p.Id, StatisticsConstants.HistoryFields.Rang, dateTime, p.Rang)
            })
            .OrderByDescending(x => x.Puissance)
            .Take(8)
            .ToList();

        var androidesAtDate = _dbContext.Personnages
            .Where(p => p.Type == TypePersonnage.Androide && p.GetType() == typeof(Personnage))
            .AsEnumerable()
            .Select(p => new
            {
                Personnage = p,
                Puissance = GetPersonnagePuissanceAtDate(p.Id, dateTime)
            })
            .OrderByDescending(x => x.Puissance)
            .Take(3)
            .ToList();

        var commandantsAtDate = _dbContext.Personnages
            .Where(p => p.Type == TypePersonnage.Commandant && p.GetType() == typeof(Personnage))
            .AsEnumerable()
            .Select(p => new
            {
                Personnage = p,
                Puissance = GetPersonnagePuissanceAtDate(p.Id, dateTime),
                Rang = GetPersonnagePropertyAtDate(p.Id, StatisticsConstants.HistoryFields.Rang, dateTime, p.Rang)
            })
            .OrderByDescending(x => x.Puissance + x.Rang * 20)
            .Take(1)
            .ToList();

        // Calculer la meilleure puissance Lucie à cette date (reconstruire depuis l'historique et les pièces)
        var bestLuciePowerAtDate = CalculateBestLuciePowerAtDate(dateTime, useCurrentState);

        var bestMercenairePower = mercenairesAtDate.Sum(m => m.Puissance);
        var bestAndroidePower = androidesAtDate.Sum(a => a.Puissance);
        var bestCommandantPower = commandantsAtDate.Any() 
            ? commandantsAtDate[0].Puissance + commandantsAtDate[0].Rang * 20 
            : 0;

        return bestMercenairePower + bestAndroidePower + bestCommandantPower + bestLuciePowerAtDate;
    }

    /// <summary>
    /// Surcharge pour DateOnly qui convertit à minuit du jour
    /// </summary>
    private int CalculateBestTeamPowerAtDate(DateOnly date)
    {
        var dateTime = date.ToDateTime(TimeOnly.MinValue);
        return CalculateBestTeamPowerAtDateForDateTime(dateTime, false);
    }

    /// <summary>
    /// Calcule la meilleure puissance Lucie possible à une date donnée
    /// </summary>
    private int CalculateBestLuciePowerAtDate(DateTime date, bool useCurrentState = false)
    {
        // Si explicitement demandé, utiliser l'état actuel
        if (useCurrentState)
        {
            return _personnageService.GetPuissanceMaxLucieEscouade();
        }

        // Chercher d'abord l'historique spécifique au max (EntiteId=-2)
        var historyMaxAtDate = _dbContext.HistoriquesModifications
            .Where(h => h.EntiteId == -2 
                     && h.TypeEntite == TypeEntite.Piece
                     && h.ChampModifie == StatisticsConstants.HistoryFields.PuissanceLucieMax
                     && h.DateModification <= date)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefault();

        if (historyMaxAtDate != null && int.TryParse(historyMaxAtDate.NouvelleValeur, out int maxValue))
            return maxValue;

        // Fallback: si pas d'historique max, utiliser la puissance sélectionnée (EntiteId=-1)
        // Cela permet d'aligner les courbes lors des imports sans détail de pièces
        var historySelectedAtDate = _dbContext.HistoriquesModifications
            .Where(h => h.EntiteId == -1 
                     && h.TypeEntite == TypeEntite.Piece
                     && h.ChampModifie == StatisticsConstants.HistoryFields.PuissanceLucieSelectionnee
                     && h.DateModification <= date)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefault();

        if (historySelectedAtDate != null && int.TryParse(historySelectedAtDate.NouvelleValeur, out int selectedValue))
            return selectedValue;

        // Pas d'historique du tout avant cette date : considérer 0 (pas de données)
        return 0;
    }

    /// <summary>
    /// Récupère la puissance d'un personnage à une date donnée
    /// </summary>
    private int GetPersonnagePuissanceAtDate(int personnageId, DateTime date)
    {
        // Chercher la dernière modification de puissance avant ou à cette date
        var lastModif = _dbContext.HistoriquesModifications
            .Where(h => h.EntiteId == personnageId 
                     && h.TypeEntite == TypeEntite.Personnage
                     && h.ChampModifie == StatisticsConstants.HistoryFields.Puissance
                     && h.DateModification <= date)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefault();

        if (lastModif != null && int.TryParse(lastModif.NouvelleValeur, out int value))
            return value;

        // Sinon, retourner la puissance actuelle
        var personnage = _dbContext.Personnages.Find(personnageId);
        return personnage?.Puissance ?? 0;
    }

    /// <summary>
    /// Récupère une propriété numérique d'un personnage à une date donnée
    /// </summary>
    private int GetPersonnagePropertyAtDate(int personnageId, string property, DateTime date, int defaultValue)
    {
        // Chercher la dernière modification avant ou à cette date
        var lastModif = _dbContext.HistoriquesModifications
            .Where(h => h.EntiteId == personnageId 
                     && h.TypeEntite == TypeEntite.Personnage
                     && h.ChampModifie == property
                     && h.DateModification <= date)
            .OrderByDescending(h => h.DateModification)
            .FirstOrDefault();

        if (lastModif != null && int.TryParse(lastModif.NouvelleValeur, out int value))
            return value;

        return defaultValue;
    }

    private void AddCurrentBestPower(List<TeamPowerEvolutionData> result)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        // Recalculer la meilleure puissance aujourd'hui en utilisant l'état actuel (pas l'historique)
        var currentBestPower = CalculateBestTeamPowerAtDateForDateTime(DateTime.Now, true);

        // Mettre à jour l'entrée du jour si elle existe, sinon l'ajouter
        var todayEntry = result.FirstOrDefault(r => r.Date == today);
        if (todayEntry != null)
        {
            todayEntry.TotalPower = currentBestPower;
        }
        else if (currentBestPower > 0)
        {
            result.Add(new TeamPowerEvolutionData
            {
                Date = today,
                TotalPower = currentBestPower
            });
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




