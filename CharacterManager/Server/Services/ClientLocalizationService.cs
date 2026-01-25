namespace CharacterManager.Server.Services;

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

/// <summary>
/// Service client pour fournir les traductions aux composants Blazor
/// </summary>
public class ClientLocalizationService
{
    private readonly ILogger<ClientLocalizationService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly LanguageContextService _languageContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Dictionary<string, object>? _currentResources;
    private string _currentLanguage = "fr";
    private readonly object _lock = new();

    public ClientLocalizationService(
        IWebHostEnvironment env, 
        ILogger<ClientLocalizationService> logger,
        LanguageContextService languageContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _env = env;
        _languageContext = languageContext;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Initialise le service avec les ressources de traduction
    /// </summary>
    public async Task InitializeAsync(string languageCode)
    {
        _currentLanguage = languageCode;
        await LoadResourcesAsync(languageCode);
    }

    /// <summary>
    /// Charge les ressources de traduction depuis le fichier JSON
    /// </summary>
    private async Task LoadResourcesAsync(string languageCode)
    {
        try
        {
            var path = Path.Combine(_env.WebRootPath, "i18n", $"{languageCode}.json");
            if (!File.Exists(path))
            {
                _logger.LogWarning("Fichier de localisation introuvable: {Path}", path);
                _currentResources = new Dictionary<string, object>();
                return;
            }

            var json = await File.ReadAllTextAsync(path);
            _currentResources = JsonSerializer.Deserialize<Dictionary<string, object>>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du chargement des ressources: {Message}", ex.Message);
            _currentResources = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Récupère une chaîne de traduction par sa clé
    /// Utilise la notation pointée : "section.key"
    /// </summary>
    public string GetKeyValue(string key)
    {
        EnsureResourcesLoaded();

        var keys = key.Split('.');
        object? current = _currentResources;

        foreach (var k in keys)
        {
            current = GetNestedValue(current, k);
            if (current == null)
            {
                return key;
            }
        }

        return ConvertToString(current, key);
    }

    /// <summary>
    /// Récupère une valeur imbriquée depuis un JsonElement ou Dictionary
    /// </summary>
    private static object? GetNestedValue(object? current, string key)
    {
        if (current is JsonElement element)
        {
            return element.TryGetProperty(key, out var prop) ? prop : null;
        }

        if (current is Dictionary<string, object> dict)
        {
            return dict.TryGetValue(key, out var value) ? value : null;
        }

        return null;
    }

    /// <summary>
    /// Convertit une valeur en chaîne de caractères
    /// </summary>
    private static string ConvertToString(object? current, string fallbackKey)
    {
        if (current is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? fallbackKey,
                JsonValueKind.Number => element.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => fallbackKey,
                _ => fallbackKey
            };
        }

        return current?.ToString() ?? fallbackKey;
    }

    /// <summary>
    /// Retourne la langue actuelle
    /// </summary>
    public string GetCurrentLanguage() => _currentLanguage;

    /// <summary>
    /// Change de langue
    /// </summary>
    public async Task SetLanguageAsync(string languageCode)
    {
        await InitializeAsync(languageCode);
    }

    /// <summary>
    /// Retourne les ressources complètes (pour debug ou accès direct)
    /// </summary>
    public Dictionary<string, object>? GetResources() => _currentResources;

    private void EnsureResourcesLoaded()
    {
        if (_currentResources != null)
        {
            return;
        }

        lock (_lock)
        {
            if (_currentResources != null)
            {
                return;
            }

            try
            {
                // Déterminer la langue à utiliser : du contexte d'utilisateur si disponible, sinon défaut
                var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? string.Empty;
                var languageToUse = _languageContext.GetLanguageForUser(username);
                
                // Mettre à jour la langue actuelle si elle a changé
                _currentLanguage = languageToUse;

                var path = Path.Combine(_env.WebRootPath, "i18n", $"{_currentLanguage}.json");
                if (!File.Exists(path))
                {
                    _logger.LogWarning("Fichier de localisation introuvable (lazy): {Path}", path);
                    _currentResources = new Dictionary<string, object>();
                    return;
                }

                var json = File.ReadAllText(path);
                _currentResources = JsonSerializer.Deserialize<Dictionary<string, object>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement lazy des ressources: {Message}", ex.Message);
                _currentResources = new Dictionary<string, object>();
            }
        }
    }
}




