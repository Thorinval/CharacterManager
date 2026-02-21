namespace CharacterManager.Server.Services;

/// <summary>
/// Représente un conflit lors de l'import (modification existante)
/// </summary>
public class ImportConflict
{
    public string PersonnageName { get; set; } = string.Empty;
    public string ChampModifie { get; set; } = string.Empty;
    public DateOnly DateClassement { get; set; }
    public object? AncienneValeur { get; set; }
    public object? NouvelleValeur { get; set; }
    public string ConflictKey => $"{PersonnageName}_{ChampModifie}_{DateClassement}";
}

/// <summary>
/// Réponse de l'utilisateur pour un conflit
/// </summary>
public class ConflictResolution
{
    public string ConflictKey { get; set; } = string.Empty;
    public bool Overwrite { get; set; } // true = écraser, false = conserver
}

/// <summary>
/// Résultat de l'import avec conflits
/// </summary>
public class ImportResultWithConflicts
{
    public bool HasConflicts => Conflicts.Count > 0;
    public List<ImportConflict> Conflicts { get; set; } = new();
    public bool CanProceed => Conflicts.Count == 0; // true si pas de conflits, prêt à procéder
}




