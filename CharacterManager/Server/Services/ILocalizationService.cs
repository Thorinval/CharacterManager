namespace CharacterManager.Server.Services;

/// <summary>
/// Interface du service de localisation pour gérer les traductions multi-langues
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Charge les ressources de traduction pour une langue donnée
    /// </summary>
    Task<Dictionary<string, object>> LoadLanguageAsync(string languageCode);
}
