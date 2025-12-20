namespace CharacterManager.Server.Models;

/// <summary>
/// Résultat d'une opération d'import de fichier
/// </summary>
public class ImportResult
{
    public bool IsSuccess { get; set; }
    public int SuccessCount { get; set; }
    public string Error { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
