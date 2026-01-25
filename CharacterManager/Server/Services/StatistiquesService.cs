using CharacterManager.Server.Data;
using CharacterManager.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace CharacterManager.Server.Services;

public class StatistiquesService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly PersonnageService _personnageService;

    public StatistiquesService(ApplicationDbContext dbContext, PersonnageService personnageService)
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
            .Where(h => h.TypeEntite == TypeEntite.Personnage && h.ChampModifie == "Niveau")
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

        // Récupérer le premier enregistrement de classement (sélection d'origine)
        var premierClassement = _dbContext.HistoriquesClassement
            .OrderBy(h => h.DateEnregistrement)
            .FirstOrDefault();

        if (premierClassement == null)
            return result;

        // Point de départ : première sélection connue
        result.Add(new TeamPowerEvolutionData
        {
            Date = premierClassement.DateEnregistrement,
            TotalPower = premierClassement.PuissanceTotale
        });

        // Récupérer l'état initial de la sélection avant le premier classement
        // Pour reconstituer qui était sélectionné à cette date
        var dateDebut = premierClassement.DateEnregistrement.ToDateTime(TimeOnly.MinValue);
        var personnagesSelectionnes = new HashSet<int>(
            _dbContext.Personnages
                .Where(p => p.Selectionne)
                .Select(p => p.Id)
        );
        
        // Appliquer les modifications antérieures au premier classement pour retrouver l'état initial
        var modificationsAntérieures = _dbContext.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage 
                     && h.ChampModifie == "Selectionne"
                     && h.DateModification <= dateDebut)
            .OrderByDescending(h => h.DateModification)
            .ToList();

        // Inverser les modifications pour revenir à l'état initial
        foreach (var modif in modificationsAntérieures)
        {
            if (modif.AncienneValeur?.ToLower() == "true")
                personnagesSelectionnes.Add(modif.EntiteId);
            else
                personnagesSelectionnes.Remove(modif.EntiteId);
        }

        int puissanceActuelle = premierClassement.PuissanceTotale;

        // Récupérer toutes les modifications après le premier classement
        var modifications = _dbContext.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage && h.DateModification > dateDebut)
            .OrderBy(h => h.DateModification)
            .ToList();

        // Grouper par date pour traiter toutes les modifications d'une même journée ensemble
        var modificationsParDate = modifications
            .GroupBy(m => DateOnly.FromDateTime(m.DateModification))
            .OrderBy(g => g.Key);

        foreach (var groupe in modificationsParDate)
        {
            bool selectionModifiee = false;
            bool puissanceModifiee = false;

            foreach (var modif in groupe)
            {
                // Vérifier si la sélection a changé
                if (modif.ChampModifie == "Selectionne")
                {
                    selectionModifiee = true;
                    // Mettre à jour l'état de la sélection
                    if (modif.NouvelleValeur?.ToLower() == "true")
                        personnagesSelectionnes.Add(modif.EntiteId);
                    else
                        personnagesSelectionnes.Remove(modif.EntiteId);
                }

                // Vérifier si une modification de puissance concerne un personnage sélectionné
                if (modif.ChampModifie == "Puissance" && personnagesSelectionnes.Contains(modif.EntiteId))
                {
                    puissanceModifiee = true;
                    // Calculer la différence de puissance
                    if (int.TryParse(modif.AncienneValeur, out int anciennePuissance) &&
                        int.TryParse(modif.NouvelleValeur, out int nouvellePuissance))
                    {
                        puissanceActuelle += (nouvellePuissance - anciennePuissance);
                    }
                }
            }

            // Ajouter un point de données si la sélection ou la puissance a changé
            if (selectionModifiee || puissanceModifiee)
            {
                result.Add(new TeamPowerEvolutionData
                {
                    Date = groupe.Key,
                    TotalPower = puissanceActuelle
                });
            }
        }

        // Ajouter les autres enregistrements de classement s'ils existent
        var autresClassements = _dbContext.HistoriquesClassement
            .Where(h => h.DateEnregistrement > premierClassement.DateEnregistrement)
            .OrderBy(h => h.DateEnregistrement)
            .ToList();

        foreach (var classement in autresClassements)
        {
            // Utiliser la valeur du classement si disponible (plus fiable)
            if (!result.Any(r => r.Date == classement.DateEnregistrement))
            {
                result.Add(new TeamPowerEvolutionData
                {
                    Date = classement.DateEnregistrement,
                    TotalPower = classement.PuissanceTotale
                });
            }
        }

        // Ajouter la puissance actuelle
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

        return result.OrderBy(r => r.Date).ToList();
    }

    public List<TeamPowerEvolutionData> GetBestTeamPowerEvolutionData()
    {
        var result = new List<TeamPowerEvolutionData>();

        // Au démarrage, la meilleure équipe = équipe sélectionnée
        var premierClassement = _dbContext.HistoriquesClassement
            .OrderBy(h => h.DateEnregistrement)
            .FirstOrDefault();

        if (premierClassement == null)
            return result;

        // Point de départ : meilleure équipe = équipe initiale
        result.Add(new TeamPowerEvolutionData
        {
            Date = premierClassement.DateEnregistrement,
            TotalPower = premierClassement.PuissanceTotale
        });

        // Récupérer l'état initial de la sélection avant le premier classement
        var dateDebut = premierClassement.DateEnregistrement.ToDateTime(TimeOnly.MinValue);
        var personnagesSelectionnes = new HashSet<int>(
            _dbContext.Personnages
                .Where(p => p.Selectionne)
                .Select(p => p.Id)
        );
        
        // Appliquer les modifications antérieures au premier classement pour retrouver l'état initial
        var modificationsAntérieures = _dbContext.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage 
                     && h.ChampModifie == "Selectionne"
                     && h.DateModification <= dateDebut)
            .OrderByDescending(h => h.DateModification)
            .ToList();

        // Inverser les modifications pour revenir à l'état initial
        foreach (var modif in modificationsAntérieures)
        {
            if (modif.AncienneValeur?.ToLower() == "true")
                personnagesSelectionnes.Add(modif.EntiteId);
            else
                personnagesSelectionnes.Remove(modif.EntiteId);
        }

        int puissanceEquipe = premierClassement.PuissanceTotale;
        int puissanceMax = premierClassement.PuissanceTotale;

        // Récupérer toutes les modifications après le premier classement
        var modifications = _dbContext.HistoriquesModifications
            .Where(h => h.TypeEntite == TypeEntite.Personnage && h.DateModification > dateDebut)
            .OrderBy(h => h.DateModification)
            .ToList();

        // Grouper par date
        var modificationsParDate = modifications
            .GroupBy(m => DateOnly.FromDateTime(m.DateModification))
            .OrderBy(g => g.Key);

        foreach (var groupe in modificationsParDate)
        {
            bool recalculerMax = false;

            foreach (var modif in groupe)
            {
                // Suivre l'état de la sélection
                if (modif.ChampModifie == "Selectionne")
                {
                    if (modif.NouvelleValeur?.ToLower() == "true")
                        personnagesSelectionnes.Add(modif.EntiteId);
                    else
                        personnagesSelectionnes.Remove(modif.EntiteId);
                }

                // Suivre la puissance de l'équipe
                if (modif.ChampModifie == "Puissance" && personnagesSelectionnes.Contains(modif.EntiteId))
                {
                    if (int.TryParse(modif.AncienneValeur, out int anciennePuissance) &&
                        int.TryParse(modif.NouvelleValeur, out int nouvellePuissance))
                    {
                        puissanceEquipe += (nouvellePuissance - anciennePuissance);
                    }
                }

                // Si le personnage modifié N'EST PAS dans l'équipe, recalculer la puissance max
                if ((modif.ChampModifie == "Puissance" || modif.ChampModifie == "Niveau" || modif.ChampModifie == "Rang") 
                    && !personnagesSelectionnes.Contains(modif.EntiteId))
                {
                    recalculerMax = true;
                }
            }

            // Mettre à jour la puissance max
            if (recalculerMax)
            {
                // La puissance max diverge de l'équipe, on doit la recalculer
                puissanceMax = _personnageService.GetPuissanceMaxEscouade();
            }
            else
            {
                // La puissance max reste celle de l'équipe
                puissanceMax = puissanceEquipe;
            }

            // Ajouter un point de données seulement si la puissance max a changé
            if (result.Last().TotalPower != puissanceMax)
            {
                result.Add(new TeamPowerEvolutionData
                {
                    Date = groupe.Key,
                    TotalPower = puissanceMax
                });
            }
        }

        // Ajouter la puissance actuelle de la meilleure équipe
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

        return result.OrderBy(r => r.Date).ToList();
    }

    #endregion

    #region Utilitaires de formatage

    public static string FormatDateWithDay(DateTime date)
    {
        var monthNames = new[] { "JAN", "FEV", "MAR", "AVR", "MAI", "JUN", "JUL", "AOU", "SEP", "OCT", "NOV", "DEC" };
        var month = monthNames[date.Month - 1];
        return $"{date.Day:D2} {month}";
    }

    public static string FormatDateForClassement(DateOnly date)
    {
        var monthNames = new[] { "JAN", "FEV", "MAR", "AVR", "MAI", "JUN", "JUL", "AOU", "SEP", "OCT", "NOV", "DEC" };
        var month = monthNames[date.Month - 1];
        return $"{date.Day:D2} {month}";
    }

    public static string GetClassementTypeLabel(TypeClassement type, Func<string, string> localizationFunction) => type switch
    {
        TypeClassement.Nutaku => localizationFunction("statistics.classementNutaku"),
        TypeClassement.Top150 => localizationFunction("statistics.classementTop150"),
        TypeClassement.France => localizationFunction("statistics.classementFrance"),
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

    public static string ColorWithAlpha(string hexColor, double alpha)
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
