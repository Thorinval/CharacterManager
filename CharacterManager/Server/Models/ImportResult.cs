using CharacterManager.Server.Services;

namespace CharacterManager.Server.Models;

/// <summary>
/// Résultat d'une opération d'import de fichier
/// </summary>
public class ImportResult
{
    public bool IsSuccess { get; set; }
    public int SuccessCount { get; set; }
    public int DuplicateCount { get; set; }
    public string? Error { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<ImportLogEntry> Logs { get; set; } = new();
    public List<ConflictResolutionApplied> ConflictsApplied { get; set; } = new();
}

/// <summary>
/// Représente une résolution de conflit qui a été appliquée
/// </summary>
public class ConflictResolutionApplied
{
    public string PersonnageName { get; set; } = string.Empty;
    public string ChampModifie { get; set; } = string.Empty;
    public DateOnly DateClassement { get; set; }
    public object? AncienneValeur { get; set; }
    public object? NouvelleValeur { get; set; }
    public bool Overwritten { get; set; } // true = nouvelle valeur appliquée, false = ancienne conservée
}
