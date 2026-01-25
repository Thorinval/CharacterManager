namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service client pour fournir les traductions aux composants Blazor
/// </summary>
public interface IClientLocalizationService
{
    /// <summary>
    /// Initialise le service avec les ressources de traduction
    /// </summary>
    Task InitializeAsync(string languageCode);

    /// <summary>
    /// Récupère une chaîne de traduction par sa clé
    /// Utilise la notation pointée : "section.key"
    /// </summary>
    string GetKeyValue(string key);

    /// <summary>
    /// Obtient la langue actuelle
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// Obtient la langue actuelle
    /// </summary>
    string GetCurrentLanguage();

    /// <summary>
    /// Change la langue et recharge les ressources
    /// </summary>
    Task ChangeLanguageAsync(string languageCode);
    
    /// <summary>
    /// Définit la langue et recharge les ressources
    /// </summary>
    Task SetLanguageAsync(string languageCode);
}
