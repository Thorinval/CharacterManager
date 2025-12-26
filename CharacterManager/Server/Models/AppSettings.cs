namespace CharacterManager.Server.Models;

using CharacterManager.Server.Constants;

public class AppSettings
{
    public int Id { get; set; }
    public string LastImportedFileName { get; set; } = string.Empty;
    public DateTime? LastImportedDate { get; set; }

    /// <summary>
    /// Mode adulte activé par défaut (permet d'afficher les images marquées comme "adulte")
    /// </summary>
    public bool IsAdultModeEnabled { get; set; } = true;

    /// <summary>
    /// Langue actuelle : "fr" pour français, "en" pour anglais
    /// </summary>
    public string Language { get; set; } = AppConstants.Defaults.DefaultLanguage;
}
