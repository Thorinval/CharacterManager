namespace CharacterManager.Server.Services;

/// <summary>
/// Service singleton pour stocker le contexte de langue pour la session HTTP actuelle
/// Cela permet au ClientLocalizationService (scoped) de connaître la langue correcte
/// même avant que LocalizationProvider n'ait terminé son initialisation
/// </summary>
public class LanguageContextService
{
    private readonly Dictionary<string, string> _languageCache = new();
    private readonly object _lock = new();
    private string _defaultLanguage = "fr";

    /// <summary>
    /// Défini la langue pour un utilisateur
    /// </summary>
    public void SetLanguageForUser(string username, string languageCode)
    {
        lock (_lock)
        {
            _languageCache[username ?? "__guest__"] = languageCode;
        }
    }

    /// <summary>
    /// Obtient la langue pour un utilisateur
    /// </summary>
    public string GetLanguageForUser(string? username)
    {
        lock (_lock)
        {
            var key = username ?? "__guest__";
            return _languageCache.TryGetValue(key, out var lang) ? lang : _defaultLanguage;
        }
    }

    /// <summary>
    /// Définit la langue par défaut globale
    /// </summary>
    public void SetDefaultLanguage(string languageCode)
    {
        lock (_lock)
        {
            _defaultLanguage = languageCode;
        }
    }

    /// <summary>
    /// Obtient la langue par défaut globale
    /// </summary>
    public string GetDefaultLanguage()
    {
        lock (_lock)
        {
            return _defaultLanguage;
        }
    }

    /// <summary>
    /// Nettoie le cache (pour testing)
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            _languageCache.Clear();
        }
    }
}
