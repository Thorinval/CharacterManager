namespace CharacterManager.Server.Models;

/// <summary>
/// Options pour l'export PML
/// Permet de gérer l'export de différentes sections de l'application de manière extensible
/// </summary>
public class PmlExportOptions
{
    // Export types constants
    public const string EXPORT_TYPE_INVENTORY = "inventory";
    public const string EXPORT_TYPE_TEMPLATES = "templates";
    public const string EXPORT_TYPE_BEST_SQUAD = "bestSquad";
    public const string EXPORT_TYPE_HISTORIES = "histories";
    public const string EXPORT_TYPE_LEAGUE_HISTORY = "leagueHistory";
    public const string EXPORT_TYPE_CAPACITES = "capacites";

    /// <summary>
    /// Sections à exporter (utilise les constantes EXPORT_TYPE_*)
    /// </summary>
    private readonly HashSet<string> _selectedExportTypes = [];

    /// <summary>
    /// Contenus supplémentaires personnalisés (dictionnaire pour extensibilité)
    /// Clé: identifiant unique du contenu
    /// Valeur: objet sérialisable contenant les données
    /// </summary>
    public Dictionary<string, object?> CustomExports { get; set; } = [];

    /// <summary>
    /// Constructeur par défaut - aucun export sélectionné
    /// </summary>
    public PmlExportOptions()
    {
    }

    /// <summary>
    /// Constructeur avec sélection initiale
    /// </summary>
    public PmlExportOptions(params string[] exportTypes)
    {
        foreach (var type in exportTypes)
        {
            _selectedExportTypes.Add(type);
        }
    }

    /// <summary>
    /// Ajoute un type d'export
    /// </summary>
    public void AddExportType(string exportType)
    {
        _selectedExportTypes.Add(exportType);
    }

    /// <summary>
    /// Retire un type d'export
    /// </summary>
    public void RemoveExportType(string exportType)
    {
        _selectedExportTypes.Remove(exportType);
    }

    /// <summary>
    /// Vérifie si un type d'export est sélectionné
    /// </summary>
    public bool IsExporting(string exportType)
    {
        return _selectedExportTypes.Contains(exportType);
    }

    /// <summary>
    /// Récupère tous les types d'export sélectionnés
    /// </summary>
    public IEnumerable<string> GetSelectedExportTypes()
    {
        return _selectedExportTypes.ToList();
    }

    /// <summary>
    /// Ajoute ou met à jour un contenu personnalisé
    /// </summary>
    public void AddCustomExport(string key, object? value)
    {
        CustomExports[key] = value;
    }

    /// <summary>
    /// Récupère un contenu personnalisé
    /// </summary>
    public object? GetCustomExport(string key)
    {
        return CustomExports.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Vérifie si au moins un export est sélectionné
    /// </summary>
    public bool HasSelectedExports()
    {
        return _selectedExportTypes.Count > 0 || CustomExports.Count > 0;
    }

    /// <summary>
    /// Réinitialise tous les exports
    /// </summary>
    public void ClearAll()
    {
        _selectedExportTypes.Clear();
        CustomExports.Clear();
    }

    /// <summary>
    /// Crée une option d'export à partir de booléens (pour rétro-compatibilité)
    /// </summary>
    public static PmlExportOptions FromBooleans(
        bool exportInventory = true,
        bool exportTemplates = true,
        bool exportBestSquad = true,
        bool exportHistories = true,
        bool exportLeagueHistory = false,
        bool exportCapacites = false)
    {
        var options = new PmlExportOptions();

        if (exportInventory)
            options.AddExportType(EXPORT_TYPE_INVENTORY);
        if (exportTemplates)
            options.AddExportType(EXPORT_TYPE_TEMPLATES);
        if (exportBestSquad)
            options.AddExportType(EXPORT_TYPE_BEST_SQUAD);
        if (exportHistories)
            options.AddExportType(EXPORT_TYPE_HISTORIES);
        if (exportLeagueHistory)
            options.AddExportType(EXPORT_TYPE_LEAGUE_HISTORY);
        if (exportCapacites)
            options.AddExportType(EXPORT_TYPE_CAPACITES);

        return options;
    }
}
