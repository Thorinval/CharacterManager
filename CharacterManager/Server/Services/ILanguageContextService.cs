namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service singleton pour stocker le contexte de langue pour la session HTTP actuelle
/// </summary>
public interface ILanguageContextService
{
    /// <summary>
    /// Défini la langue pour un utilisateur
    /// </summary>
    void SetLanguageForUser(string username, string languageCode);

    /// <summary>
    /// Obtient la langue pour un utilisateur
    /// </summary>
    string GetLanguageForUser(string? username);

    /// <summary>
    /// Définit la langue par défaut globale
    /// </summary>
    void SetDefaultLanguage(string languageCode);

    /// <summary>
    /// Obtient la langue par défaut globale
    /// </summary>
    string GetDefaultLanguage();

    /// <summary>
    /// Nettoie le cache (pour testing)
    /// </summary>
    void ClearCache();
}
